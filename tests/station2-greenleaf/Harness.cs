using WeightMaster.Models;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static double CloudNormal(GreenLeafPostModel post, int water, int mora, int thambi, int reject, int bagWeight) =>
    Math.Max(0, post.nomal_leaf_weight - water - mora - thambi - reject - bagWeight);

var sackOnly = new GreenLeafPostModel { nomal_leaf_weight = 20, box_count = 0 };
var sack = Station2Math.CalculateStation2GreenLeafTotals(sackOnly, [], 1, 1, 1, 1, 2);
Check(sack.water == 1 && sack.mora == 1 && sack.thambi == 1 && sack.reject == 1, "sack deductions unchanged");
Check(sack.green == 14 && sack.gold == 0 && CloudNormal(sackOnly, sack.water, sack.mora, sack.thambi, sack.reject, 2) == 14,
    "sack-only net leaf agrees with cloud payload");

var boxRound = new TransactionLogBlockModel
{
    box_count = 1, actual_nomal_leaf_weight = 17, final_green_leaf_count = 13,
    water = 1, morapuwata = 1, thambimata = 1, reject = 1
};
var boxOnly = new GreenLeafPostModel { nomal_leaf_weight = 17, box_count = 1 };
var box = Station2Math.CalculateStation2GreenLeafTotals(boxOnly, [boxRound], 0, 0, 0, 0, 0);
Check(box.water == 1 && box.mora == 1 && box.thambi == 1 && box.reject == 1 && box.green == 13,
    "box-only deductions and final leaf remain unchanged");

var mixedPost = new GreenLeafPostModel { nomal_leaf_weight = 37, box_count = 1 };
var mixed = Station2Math.CalculateStation2GreenLeafTotals(mixedPost, [boxRound], 1, 1, 1, 1, 2);
Check(mixed.water == 2 && mixed.mora == 2 && mixed.thambi == 2 && mixed.reject == 2,
    "mixed rounds retain all four box deductions");
Check(mixed.green == 27 && mixed.gold == 0 &&
    CloudNormal(mixedPost, mixed.water, mixed.mora, mixed.thambi, mixed.reject, 2) == 27,
    "mixed final leaf counts box once and matches cloud normal leaf");

var secondBox = new TransactionLogBlockModel
{
    box_count = 1, actual_nomal_leaf_weight = 10, final_green_leaf_count = 8, water = 2
};
var multipleBoxesPost = new GreenLeafPostModel { nomal_leaf_weight = 47, box_count = 2 };
var multipleBoxes = Station2Math.CalculateStation2GreenLeafTotals(
    multipleBoxesPost, [boxRound, secondBox], 1, 1, 1, 1, 2);
Check(multipleBoxes.water == 4 && multipleBoxes.mora == 2 && multipleBoxes.thambi == 2 &&
    multipleBoxes.reject == 2 && multipleBoxes.green == 35 &&
    CloudNormal(multipleBoxesPost, multipleBoxes.water, multipleBoxes.mora,
        multipleBoxes.thambi, multipleBoxes.reject, 2) == 35,
    "multiple box rounds retain every deduction and each box leaf once");

var cleanBox = new TransactionLogBlockModel
{
    box_count = 1, actual_nomal_leaf_weight = 17, final_green_leaf_count = 17
};
var noBoxDeductions = Station2Math.CalculateStation2GreenLeafTotals(mixedPost, [cleanBox], 1, 1, 1, 1, 2);
Check(noBoxDeductions.green == 31 &&
    CloudNormal(mixedPost, noBoxDeductions.water, noBoxDeductions.mora,
        noBoxDeductions.thambi, noBoxDeductions.reject, 2) == 31,
    "box leaf is not double-counted even without box quality deductions");

var goldBox = new TransactionLogBlockModel
{
    box_count = 1, actual_nomal_leaf_weight = 17, total_gold_leaf_weight = 3,
    final_green_leaf_count = 13, final_gold_leaf_count = 3,
    water = 1, morapuwata = 1, thambimata = 1, reject = 1
};
var goldPost = new GreenLeafPostModel { nomal_leaf_weight = 37, gold_leaf_weight = 8, box_count = 1 };
var gold = Station2Math.CalculateStation2GreenLeafTotals(goldPost, [goldBox], 1, 1, 1, 1, 2);
Check(gold.green == 27 && gold.gold == 8, "box gold is added once");

var missingBoxRejected = false;
try
{
    Station2Math.CalculateStation2GreenLeafTotals(mixedPost, [], 1, 1, 1, 1, 2);
}
catch (InvalidOperationException)
{
    missingBoxRejected = true;
}
Check(missingBoxRejected, "missing box data cannot silently produce an incomplete post");

Console.WriteLine("PASS: Station 2 green-leaf totals preserve box deductions and count box leaf once.");
