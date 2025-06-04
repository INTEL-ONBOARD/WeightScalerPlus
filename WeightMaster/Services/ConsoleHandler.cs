using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeightMaster.Core;
using WeightMaster.Models;

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

        public async Task<bool> ValidateEmail(String email)
        {
            return await _engine.VerifyEmailInDbAsync(email);
        }


        //return username or "unknown" : use this for user login 
        public async Task<string> loginUser(String email, string password)
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
        public async Task<string> GetMemberName(String id)
        {
            return await _engine.getMemberNameById(id);
        }
        //get member nuber by id
        public async Task<string> GetMemberNumber(String id)
        {
            return await _engine.getMemberNumberId(id);
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
        //return the transaction data from the station 2 data record section, this func return the records of the dataa that has weight value over 0
        //public async Task<List<TransactionLogBlockModel>> GetTransactionData(string linenames)
        //{
        //    return await _engine.GetFilteredTransactionData(linenames);
        //}
        //return the transaction data for station-1 based on barcode 
        public async Task<List<TransactionLogBlockModel>> GetTransactionDataByBarcodeId(string code)
        {
            return await _engine.GetFilteredTransactionsByBarcodeAndDateAsync(code);
        }
        //return a songle transaction data from the station 2 data using member code , barcode
        public async Task<List<TransactionLogBlockModel>> GetTransactionData(string barcode,string linename) //the line name here is useless. give this ""
        {
            return await _engine.GetFilteredTransactionData(barcode,linename);
        }
        //add a new transaction for station 2
        public async Task<bool> AddFinalTransactionAsync(FinalTransactionBlockModel newTransaction,string code)
        {
            return await _engine.SetFinalTransactionAsync(newTransaction,code);
        }
        //get data filtered based in the selected line

        public async Task<List<TransactionLogBlockModel>> getDataByFilter(string lineName)
        {
            return await _engine.GetFilteredTransactionsByLineNameAndDateAsync(lineName);
        }
        //get data filtered based in the selected line and the barcode value

        public async Task<List<TransactionLogBlockModel>> getDataByFilter(string lineName, string barcodeDetails)
        {
            return await _engine.GetFilteredTransactionsByLineNameBarcodeAndDateAsync(lineName, barcodeDetails);
        }
        ////return data for the report from thhe station-1
        //public async Task<List<FinalTransactionBlockModel>> print_sta1()
        //{
        //    return await _engine.getPrintData_1();
        //}
        //return data for the report from thhe station-2
        public async Task<List<FinalTransactionBlockModel>> print_sta2(string linename)
        {
            return await _engine.getPrintData_2(linename);
        }
        public async Task<List<FinalTransactionBlockModel>> print_sta2_onCustomDate(string linename,string date_)
        {
            return await _engine.getPrintData_2_onCustomDate(linename,date_);
        }



        //Version2 new exposed methods
        public async Task verifyMembers()
        {
            await _engine.getMemberData();
        }
        public async Task VerifyLines()
        {
            await _engine.getLineDataAsync();
        }
        public async Task SaveData(GreenLeafPostModel s)
        {
           await _engine.addPost(s);
        }
        public async Task<bool> UpdateData(int id , GreenLeafPostModel s)
        {
            return await _engine.updatePost(id ,s);
        }
        public async Task<GreenLeafPostModel?> GetData(int id)
        {
            return await _engine.getPostById(id);
        }
        public async Task<GreenLeafPostModel?> GetRecent()
        {
            return await _engine.getLatestPost();
        }
        

    }
}
