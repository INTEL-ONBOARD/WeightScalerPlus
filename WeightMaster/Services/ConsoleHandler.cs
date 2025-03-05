using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeightMaster.Core;

namespace WeightMaster.Services
{
    public class ConsoleHandler
    {
        private readonly Engine _engine;

        public ConsoleHandler()
        {
            _engine = new Engine();
        }

        public async Task GetStudentsAsync()
        {
            await _engine.dumpUserInformation();
        }

        public async Task<bool> VerifyUserDb()
        {
            return await _engine.UserDbValidation(); 
        }

        public async Task<bool> ValidateEmail(String email)
        {
            return await _engine.VerifyEmailInDbAsync(email);
        }

        public async Task<bool> loginUser(String email,string password)
        {
            return await _engine.LoginUser(email,password);
        }


    }
}
