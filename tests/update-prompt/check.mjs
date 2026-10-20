import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const source = readFileSync(fileURLToPath(new URL('../../WeightMaster/Interfaces/MainWindow.xaml.cs', import.meta.url)), 'utf8')
  .replaceAll('\r\n', '\n');
const expected = '$"Current version: v{_updateService.GetCurrentVersion().ToString(3)}\\nNew version: v{info.Version.ToString(3)}"';

// Source-wiring checks; the Windows builds validate C# compilation separately.
for (const signature of [
  'private async void AppUpdateButton_Click(',
  'private async Task RunSilentUpdateCheckAsync('
]) {
  const start = source.indexOf(signature);
  assert.notEqual(start, -1, `Missing update handler: ${signature}`);
  const end = source.indexOf('\n        private ', start + signature.length);
  assert.notEqual(end, -1, `Missing handler boundary: ${signature}`);
  const method = source.slice(start, end);
  const prompt = method.match(/MessageBox\.Show\(\s*([\s\S]*?),\s*"Update available"/);
  assert.equal(prompt?.[1], expected, `Update handler must show only the two dynamic versions: ${signature}`);
  assert.ok(!method.includes('info.Notes'), `Release notes must not appear in the popup: ${signature}`);
  assert.ok(method.includes('"Update available", MessageBoxButton.YesNo, MessageBoxImage.Information'), 'Keep the update confirmation buttons');
  assert.ok(method.includes('choice != MessageBoxResult.Yes'), 'Do not download unless the operator accepts');
  assert.ok(method.includes('await _updateService.DownloadAndApplyUpdateAsync(info'), 'Keep the existing update action');
}

console.log('PASS: manual and automatic update prompts show only current/new versions and retain Yes/No update confirmation.');
