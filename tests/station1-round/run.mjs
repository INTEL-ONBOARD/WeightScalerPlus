import { readFileSync, writeFileSync, mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const testDir = dirname(fileURLToPath(import.meta.url));
const repo = resolve(testDir, '../..');
const source = readFileSync(join(repo, 'WeightMaster/Interfaces/MainWindow.xaml.cs'), 'utf8');
const output = mkdtempSync(join(tmpdir(), 'weightmaster-round-tests-'));
const signatures = [
  'private void ResetStation1Round(', 'private void clearBtn_st1_Clicked(',
  'private async void confirmAddRowButton_st1_Click(', 'private void Window_KeyDown(',
  'private void GoToNextStep(', 'private void GoToPreviousStep(',
  'private void ShowCurrentStep(', 'private void weightScalerConfirmBtn_st1_Click(',
  'private void triggerMechanism(', 'private async void barcodeTxt_st1_TextChanged(',
  'private async void lineNameCmb_st1_SelectionChanged('
];
const methods = signatures.map(signature => {
  const start = source.indexOf(signature);
  if (start < 0) throw new Error(`Missing source method: ${signature}`);
  const end = source.indexOf('\n        private ', start + 1);
  return source.slice(start, end).replace('private async void ', 'private async Task ');
}).join('\n');
const controls = [...new Set([...methods.matchAll(/\b(\w+)\.(?:Text|SelectedIndex|SelectedValue|SelectedItem|IsEnabled|IsVisible|IsDropDownOpen|IsFocused|BorderBrush|Background|Visibility|Children|Focus|SelectAll)\b/g)]
  .map(match => match[1]).filter(name => name !== 'this' && name !== 'Keyboard' && name !== 'FocusManager')
  .concat('weightScalerConfirmBtn_st2', 'confirmAddRowButton_st2'))];
const header = source.slice(0, source.indexOf('public MainWindow()'));
const fields = [...header.matchAll(/^[ \t]*(?:private|public)\s+(?:const\s+)?(?:string|String|int|bool|double|float)\s+\w+\s*(?:=[^;\r\n]*)?;/gm)].map(match => match[0]);
writeFileSync(join(output, 'Extracted.cs'), `using System.Text;\nusing WeightMaster.Models;\npartial class RoundHarness {\n${fields.join('\n')}\n${controls.map(name => `private FakeControl ${name} = new("${name}");`).join('\n')}\n${methods}\n}`);
writeFileSync(join(output, 'Harness.cs'), readFileSync(join(testDir, 'Harness.cs')));
for (const file of ['GreenLeafpostModel.cs', 'transactionLogBlockModel.cs', 'TransportBillBlockModel.cs', 'memDbLog.cs']) {
  writeFileSync(join(output, file), readFileSync(join(repo, 'WeightMaster/Models', file)));
}
const converter = readFileSync(join(repo, 'WeightMaster/utils/BlockModelConverter.cs'), 'utf8');
const sum = converter.slice(converter.indexOf('public static GreenLeafPostModel? Sum('), converter.indexOf('public static List<TransactionLogBlockModel> filterTransactionDataByLine'));
writeFileSync(join(output, 'Converter.cs'), `using WeightMaster.Models;\nstatic class BlockModelConverter { ${sum} }`);
writeFileSync(join(output, 'Regression.csproj'), '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><WarningLevel>0</WarningLevel></PropertyGroup></Project>');
const result = spawnSync(process.env.DOTNET || 'dotnet', ['run', '--project', join(output, 'Regression.csproj'), '--verbosity', 'quiet'], { stdio: 'inherit', env: { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1' } });
if (result.error) throw result.error;
process.exitCode = result.status ?? 1;
