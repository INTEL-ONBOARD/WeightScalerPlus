using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeightMaster.Config;

namespace WeightMaster.Services
{
    public class TransportBillService
    {
        private readonly AppDbContext _context;

        public TransportBillService(AppDbContext context)
        {
            _context = context;
        }

        // get all transport bill records from the database
        public async Task<List<Models.TransportBillBlockModel>> GetAllTransportBillsAsync()
        {
            return await Task.FromResult(_context.TransportBill.ToList());
        }

        // save a new transport bill record to the database
        public async Task<bool> SaveTransportBillAsync(Models.TransportBillBlockModel transportBill)
        {
            try
            {
                await _context.TransportBill.AddAsync(transportBill);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // delete all transport bill records from the database
        public async Task DeleteAllTransportBillsAsync()
        {
            _context.TransportBill.RemoveRange(_context.TransportBill);
            await _context.SaveChangesAsync();
        }

        // get transport bill by date
        public async Task<List<Models.TransportBillBlockModel>> GetTransportBillsByDateAsync(string date)
        {
            var bills = _context.TransportBill.Where(b => b.dateCreated == date).ToList();
            return await Task.FromResult(bills);
        }

        
    }
}
