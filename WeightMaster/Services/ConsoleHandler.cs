using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeightMaster.Core;
using WeightMaster.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WeightMaster.Services
{
    public class ConsoleHandler
    {
        private readonly Engine _engine;

        public ConsoleHandler()
        {
            _engine = new Engine();
        }

        //public async Task GetStudentsAsync()
        //{
        //    await _engine.dumpUserInformation();
        //}

        public async Task<bool> VerifyUserDb()
        {
            return await _engine.UserDbValidation();
        }

        //return username or "unknown" : use this for user login 
        public async Task<string> loginUser(string email, string password)
        {
            return await _engine.LoginUser(email, password);
        }

        //return usernames of all users as a list
        public async Task<List<string>> getUsernames()
        {
            return await _engine.GetUsernamesAsync();
        }



        //verify the lineMasterdb
        public async Task VerifyLineMasterDb()
        {
            await _engine.DumpLineMastersInformationAsync();
        }

        //get data from the linemaster db return as a list -v2
        public async Task<List<LineBlockModel>> getLineMasterData()
        {
            return await _engine.getLineMasterData();
        }

        //verify the memberdb
        public async Task verifyMemberDb()
        {
            await _engine.DumpMemberInformation();
        }
        //get member name by id - v2
        public async Task<string> GetMemberName(string id)
        {
            return await _engine.getMemberNameById(id);
        }
        //set and update the local and cloud database with the finish button process

        public async Task<bool> AddTransactionAsync(TransactionLogBlockModel newTransaction)
        {
            return await _engine.setTransaction(newTransaction);
        }
        //method to check and verify the local database get synced with the cloud database
        public async Task<bool> verifyTransactionsCloudCheck()
        {
            return await _engine.verifyTransactionsCloud();
        }
        //add a new transaction for station 2
        public async Task<bool> AddFinalTransactionAsync(FinalTransactionBlockModel newTransaction, string code)
        {
            return await _engine.SetFinalTransactionAsync(newTransaction, code);
        }
        //get data filtered based in the selected line

        public async Task<List<TransactionLogBlockModel>> getDataByFilter(string lineName)
        {
            return await _engine.GetFilteredTransactionsByLineNameAndDateAsync(lineName);
        }
        //get data filtered based in the selected line and the barcode value

        //get all the transactipn that are compelted from the station 1
        public async Task<List<TransactionLogBlockModel>> getCompletedDataByFilter(string lineName)
        {
            return await _engine.GetCompletedFilteredTransactionsByLineNameAndDateAsync(lineName);
        }

        public async Task<List<TransactionLogBlockModel>> getDataByFilter(string lineName, string barcodeDetails)
        {
            return await _engine.GetFilteredTransactionsByLineNameBarcodeAndDateAsync(lineName, barcodeDetails);
        }


        public async Task<List<TransactionLogBlockModel>> getPendingTransactionBagData(string lineName, string barcodeDetails)
        {
            return await _engine.GetPendingTransactionsByLineBarcodeDateAsync(lineName, barcodeDetails);
        }


        public async Task<List<FinalTransactionBlockModel>> print_sta2_onCustomDate(string linename, string date_)
        {
            return await _engine.getPrintData_2_onCustomDate(linename, date_);
        }



        //Version2 new exposed methods
        public async Task verifyMembers()
        {
            await _engine.getMemberData();
        }
        public async Task VerifyLines(int id)
        {
            await _engine.getLineDataAsync(id);
        }


        //station 1 use only
        public async Task SaveData(GreenLeafPostModel s)
        {
            await _engine.addPost(s);
        }
        //update the SaveData savedd on st1 record by using this at st2
        public async Task<bool> UpdateData(int id, GreenLeafPostModel s)
        {
            return await _engine.updatePost(id, s);
        }

        public async Task<GreenLeafPostModel> getDatabyMemberiDandDateSingle(string memberid, string date_)
        {
            return await _engine.getPostByMemberAndDate(memberid, date_);
        }
        //to get the pre-member number //also replacable with getMemberDetails
        public async Task<memDbLog> getMember(string customMemberNum)
        {
            return await _engine.GetMemberByCustomMemberNumAsync(customMemberNum);
        }
        public async Task cloudsync()
        {
            await _engine.cloudSync();
        }
        // report data for boxes with line name and date from station 1 for daily report
        public async Task<List<TransactionLogBlockModel>> DocumentPrintWithDateAndLinename(string lineName, string date_)
        {
            return await _engine.getDataForDocumentsWithDate(lineName, date_);
        }
        // Document Print Methods for boxes with date
        public async Task<List<TransactionLogBlockModel>> DocumentPrintWithDate(string date_)
        {
            return await _engine.getDataForDocuments(date_);
        }

        //Station 1 box data that didn't go to staion 2: For line-wise report
        public async Task<List<TransactionLogBlockModel>> GetBoxOnlyLineReportData(string lineName, string date_)
        {
            return await _engine.getCustomDataOut(lineName, date_);
        }

        public async Task<List<GreenLeafPostModel>> getAllpostsbyFilteringExtended(string memberid, string line)
        {
            return await _engine.GetAllPostsByFilteringPost(memberid, line);
        }

        //get all transport bills
        //public async Task<List<TransportBillBlockModel>> getAllTransportBills()
        //{
        //    return await _engine.getAllTransportBillsAsync();
        //}

        // get transport bills by date
        public async Task<List<TransportBillBlockModel>> getAllTransportBills(string? date = null)   // ← note the ? for default date parameter
        {
            date ??= DateTime.Today.ToString("yyyy-MM-dd");
            return await _engine.getTransportBillsByDateAsync(date);
        }

        // add a transport bill
        public async Task<bool> addTransportBill(TransportBillBlockModel newBill)
        {
            if(newBill == null || newBill.billNo == null || newBill.billNo == "" || newBill.billNo == "-") 
            {
                return true;
            }
            return await _engine.addTransportBillAsync(newBill);

        }

        // get transport bill by line name
        public async Task<string?> getTransportBillNoByLineName(string lineName, string date = null)
        {
            date ??= DateTime.Today.ToString("yyyy-MM-dd");
            return await _engine.GetTransportBillNumberByLineNameAsync(lineName, date);
        }

        // Get DbContext for Transaction View and Line Summary features
        public Config.AppDbContext GetDbContext()
        {
            return new Config.AppDbContext();
        }


        // transaction view data
        public async Task<List<FinalTransactionBlockModel>> printTransactionByDate_trans(string date_)
        {
            return await _engine.getTransDataByDate(date_);
        }
        // transaction view data
        public async Task<List<TransactionLogBlockModel>> printTransactionsBoxAndPendingBagsByDate_trans( string date_)
        {
            return await _engine.getBoxOnlyTransactions(date_);
        }
    }
}
