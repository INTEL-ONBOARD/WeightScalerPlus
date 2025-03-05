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
    }
}
