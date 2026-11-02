import assert from 'node:assert/strict';
import { readFileSync, writeFileSync, mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const testDir = dirname(fileURLToPath(import.meta.url));
const repo = resolve(testDir, '../..');
const source = readFileSync(join(repo, 'WeightMaster/Interfaces/MainWindow.xaml.cs'), 'utf8');
const engine = readFileSync(join(repo, 'WeightMaster/Core/Engine.cs'), 'utf8');
const output = mkdtempSync(join(tmpdir(), 'weightmaster-station2-greenleaf-'));

const signatures = [
  'private static (int green, int gold) ComputeNetLeafSt2(',
  'private static (int water, int mora, int thambi, int reject, int green, int gold)'
];
const methods = signatures.map(signature => {
  const start = source.indexOf(signature);
  assert.notEqual(start, -1, `Missing source method: ${signature}`);
  const end = source.indexOf('\n        private ', start + signature.length);
  assert.notEqual(end, -1, `Missing method boundary: ${signature}`);
  return source.slice(start, end).replace('private static ', 'public static ');
}).join('\n');

const station2 = source.slice(source.indexOf('private async void confirmAddRowButton_st2_Click('),
  source.indexOf('\n        private ', source.indexOf('private async void confirmAddRowButton_st2_Click(') + 1));
assert.ok(station2.includes('CalculateStation2GreenLeafTotals('));
assert.ok(station2.indexOf('CalculateStation2GreenLeafTotals(') < station2.indexOf('AddFinalTransactionAsync('),
  'Box totals must be validated before writing the final transaction');
for (const [field, result] of [
  ['wathurata', 'water'], ['morapuwata', 'mora'], ['thambimata', 'thambi'],
  ['rejected', 'reject'], ['final_green_leaf_count', 'green'], ['final_gold_leaf_count', 'gold']
]) {
  assert.ok(station2.includes(`${field} = postTotals_st2.${result}`), `Station 2 must store calculated ${field}`);
}
for (const field of ['wathurata', 'morapuwata', 'thambimata', 'rejected']) {
  assert.ok(engine.includes(`${field} = postModel.${field}`), `Cloud payload must include ${field}`);
}

writeFileSync(join(output, 'Extracted.cs'), `using WeightMaster.Models;\nstatic class Station2Math {\n${methods}\n}`);
writeFileSync(join(output, 'Harness.cs'), readFileSync(join(testDir, 'Harness.cs')));
for (const file of ['GreenLeafpostModel.cs', 'transactionLogBlockModel.cs']) {
  writeFileSync(join(output, file), readFileSync(join(repo, 'WeightMaster/Models', file)));
}
writeFileSync(join(output, 'Regression.csproj'), '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>');

const result = spawnSync(process.env.DOTNET || 'dotnet', ['run', '--project', join(output, 'Regression.csproj'), '--verbosity', 'quiet'], {
  stdio: 'inherit', env: { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1' }
});
if (result.error) throw result.error;
process.exitCode = result.status ?? 1;
