using Microsoft.EntityFrameworkCore;
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

        // validate if a transport bill with the same bill number already exists
        public async Task<bool> TransportBillExistsAsync(string billNo)
        {
            var exists = _context.TransportBill.Any(b => b.billNo == billNo);
            return await Task.FromResult(exists);
        }

        // delete all transport bill records from the database
        public async Task DeleteAllTransportBillsAsync()
        {
            _context.TransportBill.RemoveRange(_context.TransportBill);
            await _context.SaveChangesAsync();
        }

        // get transport bills by date
        public async Task<List<Models.TransportBillBlockModel>> GetTransportBillsByDateAsync(string date)
        {
            var bills = _context.TransportBill.Where(b => b.dateCreated == date).ToList();
            return await Task.FromResult(bills);
        }

        // get the latest transport bill number by line name and date
        public async Task<string?> GetTransportBillNoByLineNameAsync(string lineName, string date)
        {
            var billNo = await _context.TransportBill
                .Where(b => b.lineName == lineName && b.dateCreated == date)
                .OrderByDescending(b => b.Id)   // newest by highest Id
                .Select(b => b.billNo)
                .FirstOrDefaultAsync();

            return billNo;
        }


    }
}
