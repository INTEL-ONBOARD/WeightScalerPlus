using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeightMaster.Config;
using WeightMaster.Models;
using WeightMaster.Services;

namespace WeightMaster.Core
{
    public class Engine
    {
        //public async Task dumpUserInformation()
        //{
        //    ApiClient apiClient = new ApiClient();
        //    string url = "http://152.42.249.231:8000/api/method/send_user_infromtaion";
        //    userModel apiResponse = await apiClient.PostAsync<userModel>(url, new {});

        //    if (apiResponse != null && apiResponse.Status == "success")
        //    {
        //        foreach (var user in apiResponse.Users)
        //        {
        //            Console.WriteLine($"Username: {user.Username}");
        //            Console.WriteLine($"Email: {user.Email}");
        //            Console.WriteLine($"Full Name: {user.FullName}");
        //            Console.WriteLine("Roles:");
        //            foreach (var role in user.Roles)
        //            {
        //                Console.WriteLine($"- {role}");
        //            }
        //            Console.WriteLine($"API Key: {user.ApiKey}");
        //            Console.WriteLine($"API Secret: {user.ApiSecret}");
        //            Console.WriteLine();
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("No data received or status is not 'success'.");
        //    }
        //}

        public async Task dumpUserInformation()
        {
            ApiClient apiClient = new ApiClient();
            string url = "http://152.42.249.231:8000/api/method/send_user_infromtaion";

            // Pass null since no data is needed
            userModel apiResponse = await apiClient.PostAsync<userModel>(url, null);
            

            //var userService = new UserService(new AppDbContext());
            //await userService.SaveUsersAsync(apiResponse.Users);
            if (apiResponse != null && apiResponse.Status == "success")
            {

                var userService = new UserService(new AppDbContext());
                await userService.SaveUsersAsync(apiResponse.Users); 


                foreach (var user in apiResponse.Users)
                {
                    System.Diagnostics.Debug.WriteLine($"Username: {user.Username}");
                    System.Diagnostics.Debug.WriteLine($"Email: {user.Email}");
                    System.Diagnostics.Debug.WriteLine($"Full Name: {user.FullName}");
                    System.Diagnostics.Debug.WriteLine("Roles:");
                    foreach (var role in user.Roles)
                    {
                        System.Diagnostics.Debug.WriteLine($"- {role}");
                    }
                    System.Diagnostics.Debug.WriteLine($"API Key: {user.ApiKey}");
                    System.Diagnostics.Debug.WriteLine($"API Secret: {user.ApiSecret}");

                }
            }
            else
            {
                Console.WriteLine("No data received or status is not 'success'.");
            }
        }


    }
}
