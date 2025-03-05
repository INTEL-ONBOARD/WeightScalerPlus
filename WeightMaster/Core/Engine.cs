using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeightMaster.Models;

namespace WeightMaster.Core
{
    public class Engine
    {
        public async Task dumpUserInformation()
        {
            ApiClient apiClient = new ApiClient();
            string url = "http://152.42.249.231:8000/api/method/send_user_infromtaion";
            userModel apiResponse = await apiClient.PostAsync<userModel>(url, new {});

            if (apiResponse != null && apiResponse.Status == "success")
            {
                foreach (var user in apiResponse.Users)
                {
                    Console.WriteLine($"Username: {user.Username}");
                    Console.WriteLine($"Email: {user.Email}");
                    Console.WriteLine($"Full Name: {user.FullName}");
                    Console.WriteLine("Roles:");
                    foreach (var role in user.Roles)
                    {
                        Console.WriteLine($"- {role}");
                    }
                    Console.WriteLine($"API Key: {user.ApiKey}");
                    Console.WriteLine($"API Secret: {user.ApiSecret}");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No data received or status is not 'success'.");
            }
        }
    }
}
