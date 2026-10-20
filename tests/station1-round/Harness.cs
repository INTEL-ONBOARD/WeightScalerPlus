using System.Text;
using WeightMaster.Models;

await RoundHarness.Run();

partial class RoundHarness
{
    private FakeService _consoleHandler = new();
    private FakeConfig _appConfig = new();
    private FakeTimer _timer = new();
    private StringBuilder _barcodeBuffer = new();
    private memDbLog memberData_st1 = new();
    private GreenLeafPostModel greenLeafPostModel_st1 = new();
    private List<FakeLine> lineMasterData = [new("Haggala", "Agent A"), new("Katujurunda", "Agent B")];
    private bool ValidateCurrentStep() => true;
    private string GetLineIdByLineName(string name) => name == "Haggala" ? "1" : "2";
    private void UpdateAllLinesTotalRow_st1() { }
    private bool Focus() { Keyboard.Current = "window"; return true; }
    private void LoadStep1_st1() { }
    private void LoadStep2_st1() { }
    private void LoadStep3_st1() { }
    private void LoadStep4_st1() { }
    private void LoadStep5_st1() { }
    private void LoadStep1_st2() { }
    private void LoadStep2_st2() { }
    private void LoadStep3_st2() { }
    private void LoadStep4_st2() { }
    private void weightScalerConfirmBtn_st2_Click(object sender, RoutedEventArgs e) { }
    private void confirmAddRowButton_st2_Click(object sender, RoutedEventArgs e) { }

    private RoundHarness()
    {
        Station1Frame.IsVisible = true;
        Station2Frame.IsVisible = false;
        weightLeafOfficerTxt_st1.Text = "Operator";
        ResetStation1Round();
    }

    private KeyEventArgs Enter(bool repeat = false)
    {
        var key = new KeyEventArgs { Key = Key.Enter, IsRepeat = repeat };
        Window_KeyDown(this, key);
        return key;
    }

    private void ReadyScale()
    {
        weightScalerConfirmBtn_st1.IsEnabled = false;
        weightScalerStatus_st1.Text = "සමබරයි";
        weightScalerValTxt_st1.Text = "0";
        triggerMechanism(this, new());
        weightScalerValTxt_st1.Text = "23.8";
        triggerMechanism(this, new());
    }

    private void Fill(string member = "12345", string line = "Haggala")
    {
        lineNameCmb_st1.SelectedValue = line;
        supervisorCmb_st1.SelectedValue = "Supervisor";
        billNoTxt_st1.Text = "Bill-42";
        barcodeTxt_st1.Text = member;
        memberId_st1 = member;
        nSacksTxt_st1.Text = "1";
        acceptedLeafWeightTxt_st1.Text = "23";
        normalLeafWeightTxt_st1.Text = "23";
        currentAcceptedLeafWeight_st1 = 23;
        currentNormalLeafWeight_st1 = 23;
        scalerRoundedWeight_st1 = 23;
        confirmAddRowButton_st1.IsEnabled = true;
    }

    private void AssertFresh()
    {
        Check(currentStep_st1 == 1, "round returns to step 1");
        Check(barcodeTxt_st1.Text == "" && memberId_st1 == "", "member cleared");
        Check(lineNameCmb_st1.SelectedValue == null && supervisorCmb_st1.SelectedValue == null, "line and supervisor cleared");
        Check(billNoTxt_st1.Text == "" && customerNameTxt_st1.Text == "-" && lineMasterNameLbl_st1.Text == "-", "dependent metadata cleared");
        Check(nSacksTxt_st1.Text == "" && nBoxesTxt_st1.Text == "" && acceptedLeafWeightTxt_st1.Text == "", "weighing cleared");
        Check(currentAcceptedLeafWeight_st1 == 0 && scalerRoundedWeight_st1 == 0, "cached weights cleared");
        Check(MemberTurnTablePanel_st1.Children.Count == 0 && LineTablePanel_st1.Children.Count == 0, "context tables cleared");
        Check(!confirmAddRowButton_st1.IsEnabled && !_isSavingStation1, "ready for a new round");
        Check(Keyboard.Current == "window", "keyboard focus restored");
        Check(weightLeafOfficerTxt_st1.Text == "Operator", "logged-in officer retained");
    }

    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
    }

    public static async Task Run()
    {
        var h = new RoundHarness();
        for (int round = 0; round < 2; round++)
        {
            h.ReadyScale();
            Check(h.Enter().Handled && h.currentStep_st1 == 3, "first Enter reads scale and focuses barcode");
            h.Fill(round == 0 ? "12345" : "54321", round == 0 ? "Haggala" : "Katujurunda");
            h.Enter(); Check(h.currentStep_st1 == 4, "barcode to sacks");
            h.Enter(); Check(h.currentStep_st1 == 5, "sacks to deductions");
            h.Enter(); Check(h.currentStep_st1 == 6 && Keyboard.Current == "confirmAddRowButton_st1", "deductions to Confirm");
            h.Enter();
            h.AssertFresh();
        }
        Check(h._consoleHandler.Transactions.Count == 2 && h._consoleHandler.Posts.Count == 2, "two persisted rounds retained");
        Check(h._consoleHandler.Transactions[1].barcode_details == "54321" && h._consoleHandler.Transactions[1].linename == "Katujurunda", "second round uses fresh selections");
        Check(Logger.Events.Count(name => name == "station1_save") == 2, "successful saves retain production event logging");
        Console.WriteLine("PASS: two complete five-Enter rounds clear metadata and retain both saved records.");

        var unregistered = new RoundHarness();
        unregistered._consoleHandler.UnregisteredMember = true;
        unregistered.Fill("99999");
        await unregistered.confirmAddRowButton_st1_Click(unregistered, new());
        unregistered.AssertFresh();
        Check(unregistered._consoleHandler.Transactions.Count == 1 && unregistered._consoleHandler.Posts.Count == 1, "unregistered member still saves transaction and green-leaf post");
        Check(unregistered._consoleHandler.Posts[0].membernumber == "99999", "unregistered member is not attributed to a previous member");
        Console.WriteLine("PASS: unregistered-member support and production save logging are preserved.");

        h.Fill();
        h._consoleHandler.Gate = new();
        var first = h.confirmAddRowButton_st1_Click(h, new());
        int calls = h._consoleHandler.AddCalls;
        var duplicate = h.confirmAddRowButton_st1_Click(h, new());
        Check(h._consoleHandler.AddCalls == calls, "duplicate save blocked before await");
        Check(h.Enter().Handled && h._isSavingStation1, "Enter blocked while saving");
        h.clearBtn_st1_Clicked(h, new());
        Check(h.barcodeTxt_st1.Text == "12345", "Clear cannot erase an in-flight save");
        h._consoleHandler.Gate.SetResult(true);
        await Task.WhenAll(first, duplicate).WaitAsync(TimeSpan.FromSeconds(3));
        h._consoleHandler.Gate = null;
        h.AssertFresh();
        Console.WriteLine("PASS: duplicate Enter/save and Clear during save cannot interrupt the next round.");

        h.Fill(); h._consoleHandler.FailSave = true;
        await h.confirmAddRowButton_st1_Click(h, new());
        Check(h.barcodeTxt_st1.Text == "12345" && h.nSacksTxt_st1.Text == "1" && h.lineNameCmb_st1.SelectedValue != null, "failed save keeps all inputs");
        Check(h.currentStep_st1 == 6 && h.confirmAddRowButton_st1.IsEnabled && !h._isSavingStation1 && h.Station1Frame.IsEnabled, "failed save restores controls");
        Check(Logger.Errors.Contains("Station1.Confirm"), "save failures retain production error logging");
        Console.WriteLine("PASS: failed save preserves the form and restores keyboard confirmation.");

        h._consoleHandler.FailSave = false;
        h.ResetStation1Round(); h.Fill();
        h.ReadyScale(); h.Enter();
        int step = h.currentStep_st1;
        Check(h.Enter(true).Handled && h.currentStep_st1 == step, "key auto-repeat cannot skip a step");
        int version = h._station1RoundVersion;
        h.clearBtn_st1_Clicked(h, new());
        h.AssertFresh();
        Check(h._station1RoundVersion > version, "reset invalidates old async lookups");
        Check(!h.weightScalerConfirmBtn_st1.IsEnabled, "reset preserves zero-return interlock");
        Console.WriteLine("PASS: Clear uses the same fresh reset; stale requests and key auto-repeat are invalidated.");

        var table = new RoundHarness();
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        TransactionLogBlockModel Entry(int id, string? member) => new()
        {
            Id = id, barcode_details = member, linename = "Haggala", date = today,
            bag_count = id / 10, actual_nomal_leaf_weight = id
        };
        table._consoleHandler.TableTransactions =
            [Entry(30, "00001"), Entry(10, "00001"), Entry(40, "00002"), Entry(20, "00002")];
        table.lineNameCmb_st1.SelectedValue = "Haggala";
        await table.lineNameCmb_st1_SelectionChanged(table, new());
        var lineRows = table.LineTablePanel_st1.Children.Cast<Station1LineTableRow>().ToList();
        Check(lineRows.Select(row => row.Bags).SequenceEqual(new[] { "4", "3", "2", "1" }), "line table newest first, independent of database return order");
        Check(lineRows.Select(row => row.Round).SequenceEqual(new[] { "2", "2", "1", "1" }), "chronological round numbers preserved for interleaved members");
        Check(table.lineRowNSacks_st1.Text == "10" && table.lineRowTotalLeafWeights_st1.Text == "100", "sorting preserves totals");
        table.barcodeTxt_st1.Text = "1";
        await table.barcodeTxt_st1_TextChanged(table, new());
        var memberRows = table.LineTablePanel_st1.Children.Cast<Station1LineTableRow>().ToList();
        Check(memberRows.Select(row => row.Bags).SequenceEqual(new[] { "3", "1" }), "member table newest first");
        Check(memberRows.Select(row => row.Round).SequenceEqual(new[] { "2", "1" }), "member rounds are not renumbered by descending display");
        Check(table.lineRowNSacks_st1.Text == "4" && table.lineRowTotalLeafWeights_st1.Text == "40", "member totals unchanged");
        Console.WriteLine("PASS: both table-loading paths sort newest first and preserve per-member rounds and totals.");

        table.ResetStation1Round();
        table._consoleHandler.TableTransactions = [Entry(30, null), Entry(10, ""), Entry(20, "00001")];
        table.lineNameCmb_st1.SelectedValue = "Haggala";
        await table.lineNameCmb_st1_SelectionChanged(table, new());
        var blankRows = table.LineTablePanel_st1.Children.Cast<Station1LineTableRow>().ToList();
        Check(blankRows.Select(row => row.Round).SequenceEqual(new[] { "2", "1", "1" }), "null and empty member identifiers stay in one round-count group");
        Console.WriteLine("PASS: newest-first sorting handles blank member identifiers consistently.");

        var shortcut = new RoundHarness();
        foreach (int originalStep in new[] { 1, 3, 4, 5, 6 })
        {
            shortcut.Fill();
            shortcut.currentStep_st1 = originalStep;
            Keyboard.Current = "nSacksTxt_st1";
            var m = new KeyEventArgs { Key = Key.M };
            int selections = shortcut.billNoTxt_st1.SelectAllCalls;
            shortcut.Window_KeyDown(shortcut, m);
            Check(m.Handled && Keyboard.Current == "billNoTxt_st1", "M focuses bill number from any round step");
            Check(shortcut.billNoTxt_st1.SelectAllCalls == selections + 1, "M selects the current bill number");
            Check(shortcut.currentStep_st1 == originalStep, "M does not advance or reset the round");
            shortcut.billNoTxt_st1.Text = "1042";
            int saves = shortcut._consoleHandler.AddCalls;
            Check(shortcut.Enter().Handled && Keyboard.Current == "barcodeTxt_st1" && shortcut.currentStep_st1 == 3, "Enter returns to member number");
            Check(shortcut.billNoTxt_st1.Text == "1042" && shortcut.acceptedLeafWeightTxt_st1.Text == "23" && shortcut.nSacksTxt_st1.Text == "1", "bill shortcut preserves entered round data");
            Check(shortcut._consoleHandler.AddCalls == saves, "bill Enter never saves the round");
        }
        Keyboard.Current = "barcodeTxt_st1";
        var oldShortcut = new KeyEventArgs { Key = Key.N };
        shortcut.Window_KeyDown(shortcut, oldShortcut);
        Check(!oldShortcut.Handled && Keyboard.Current == "barcodeTxt_st1", "N no longer activates the bill shortcut");
        shortcut.Window_KeyDown(shortcut, new() { Key = Key.M, IsRepeat = true });
        Check(Keyboard.Current == "barcodeTxt_st1", "repeated M does not steal focus");
        foreach (var modifiers in new[] { ModifierKeys.Control, ModifierKeys.Alt, ModifierKeys.Windows })
        {
            Keyboard.Modifiers = modifiers;
            var modifiedM = new KeyEventArgs { Key = Key.M };
            shortcut.Window_KeyDown(shortcut, modifiedM);
            Check(!modifiedM.Handled && Keyboard.Current == "barcodeTxt_st1", "modified M is left to the application/OS");
        }
        Keyboard.Modifiers = ModifierKeys.None;
        shortcut.lineNameCmb_st1.IsDropDownOpen = true;
        shortcut.supervisorCmb_st1.IsDropDownOpen = true;
        Keyboard.Modifiers = ModifierKeys.Shift;
        shortcut.Window_KeyDown(shortcut, new() { Key = Key.M });
        Check(Keyboard.Current == "billNoTxt_st1" && !shortcut.lineNameCmb_st1.IsDropDownOpen && !shortcut.supervisorCmb_st1.IsDropDownOpen, "uppercase M focuses bill and closes dropdowns");
        shortcut.Enter();
        Keyboard.Modifiers = ModifierKeys.None;
        shortcut._isSavingStation1 = true;
        var busyM = new KeyEventArgs { Key = Key.M };
        shortcut.Window_KeyDown(shortcut, busyM);
        Check(busyM.Handled && Keyboard.Current == "barcodeTxt_st1", "M is blocked during save");
        shortcut._isSavingStation1 = false;
        shortcut.Station1Frame.IsVisible = false;
        shortcut.Station2Frame.IsVisible = true;
        var station2M = new KeyEventArgs { Key = Key.M };
        shortcut.Window_KeyDown(shortcut, station2M);
        Check(!station2M.Handled && Keyboard.Current == "barcodeTxt_st1", "shortcut is limited to Station 1");
        Console.WriteLine("PASS: M selects the bill number; Enter returns to member number; N no longer activates the shortcut.");
    }
}

sealed class FakeControl(string name)
{
    public string Text { get; set; } = "";
    public object? SelectedValue { get; set; }
    public object? SelectedItem => SelectedValue;
    public int SelectedIndex { set { if (value == -1) SelectedValue = null; } }
    public bool IsEnabled { get; set; } = true;
    public bool IsVisible { get; set; }
    public bool IsDropDownOpen { get; set; }
    public bool IsFocused => Keyboard.Current == name;
    public bool IsKeyboardFocusWithin => IsFocused;
    public object? Background { get; set; }
    public object? BorderBrush { get; set; }
    public Visibility Visibility { get; set; }
    public List<object> Children { get; } = [];
    public bool Focus() { Keyboard.Current = name; return true; }
    public int SelectAllCalls { get; private set; }
    public void SelectAll() => SelectAllCalls++;
}
sealed class FakeService
{
    public List<TransactionLogBlockModel> Transactions = [];
    public List<GreenLeafPostModel> Posts = [];
    public List<TransactionLogBlockModel> TableTransactions = [];
    public TaskCompletionSource<bool>? Gate;
    public bool FailSave;
    public bool UnregisteredMember;
    public int AddCalls;
    public Task<memDbLog?> getMember(string member) => Task.FromResult(UnregisteredMember ? null : new memDbLog { CustomMemberNum = member });
    public Task<string> GetMemberName(string member) => Task.FromResult("M. Perera");
    public Task<GreenLeafPostModel?> getDatabyMemberiDandDateSingle(string member, string date) => Task.FromResult<GreenLeafPostModel?>(null);
    public Task<string> getTransportBillNoByLineName(string line) => Task.FromResult("Bill-42");
    public Task<List<TransactionLogBlockModel>> getDataByFilter(string line) => Task.FromResult(TableTransactions);
    public async Task<bool> AddTransactionAsync(TransactionLogBlockModel transaction)
    {
        AddCalls++;
        if (Gate != null) await Gate.Task;
        if (FailSave) return false;
        Transactions.Add(transaction); return true;
    }
    public Task<bool> addTransportBill(TransportBillBlockModel bill) => Task.FromResult(true);
    public Task<List<GreenLeafPostModel>> getAllpostsbyFilteringExtended(string member, string line) => Task.FromResult(Posts.Where(p => p.membernumber == member && p.transportlinename == line).ToList());
    public Task<bool> SaveData(GreenLeafPostModel post) { post.id = Posts.Count + 1; Posts.Add(post); return Task.FromResult(true); }
    public Task<bool> UpdateData(int id, GreenLeafPostModel post) { post.id = id; Posts[Posts.FindIndex(p => p.id == id)] = post; return Task.FromResult(true); }
    public Task<bool> verifyTransactionsCloudCheck() => Task.FromResult(true);
}
sealed record FakeLine(string LineName, string LineMaster);
sealed class FakeConfig { public string branchName = "Factory"; public string branchId = "1"; }
sealed class FakeTimer { public void Stop() { } }
enum Key { Enter, Escape, N, M }
[Flags] enum ModifierKeys { None = 0, Control = 1, Alt = 2, Windows = 4, Shift = 8 }
enum Visibility { Visible, Hidden }
sealed class KeyEventArgs { public Key Key; public bool Handled; public bool IsRepeat; }
sealed class RoutedEventArgs { }
sealed class TextChangedEventArgs { }
sealed class SelectionChangedEventArgs { }
sealed record Station1LineTableRow(string Round, string Member, string Bags, string Boxes, string Golden, string Normal, string Total);
readonly record struct Color;
sealed class SolidColorBrush(Color value) { }
static class ColorConverter { public static Color ConvertFromString(string value) => new(); }
static class Keyboard { public static string? Current; public static ModifierKeys Modifiers; public static void ClearFocus() => Current = null; }
static class FocusManager { public static object GetFocusScope(object control) => new(); public static void SetFocusedElement(object scope, object? element) { } }
static class MessageBox { public static void Show(params object[] args) { } }
enum MessageBoxButton { OK }
enum MessageBoxImage { Information, Error }
static class ConsoleSound { public static void PlayStable() { } }
static class Logger
{
    public static List<string> Events { get; } = [];
    public static List<string> Errors { get; } = [];
    public static void Event(string name, object? data = null) => Events.Add(name);
    public static void Error(string source, string message, Exception? error = null, object? context = null) => Errors.Add(source);
}
