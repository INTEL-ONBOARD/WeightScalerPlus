using System;
using System.Collections.Generic;
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

        //get data from the linemaster db return as a list
        public async Task<List<LineMasterBlockModel>> getLineMasterData()
        {
            return await _engine.getLineMasterData();
        }
    }
}
