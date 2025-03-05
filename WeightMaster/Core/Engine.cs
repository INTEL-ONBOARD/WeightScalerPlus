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
        public async Task dumpUserInformation()
        {
            ApiClient apiClient = new ApiClient();
            string url = "http://152.42.249.231:8000/api/method/send_user_infromtaion";

            userModel apiResponse = await apiClient.PostAsync<userModel>(url, null);

            if (apiResponse != null && apiResponse.Status == "success")
            {

                var userService = new UserService(new AppDbContext());
                await userService.ReplaceUsersAsync(apiResponse.Users);


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
        public async Task<int> getChangeCount()
        {
            ApiClient apiClient = new ApiClient();
            string url = "http://152.42.249.231:8000/api/method/send_user_infromtaion";

            userModel apiResponse = await apiClient.PostAsync<userModel>(url, null);
            int userCount = 0;
            if (apiResponse != null && apiResponse.Status == "success")
            {
                foreach (var user in apiResponse.Users)
                {
                   userCount++;
                }
            }
            else
            {
                Console.WriteLine("No data received or status is not 'success'.");
            }
            return userCount;
        }
        public async Task<bool> UserDbValidation()
        {
            try {
                await Task.Run(async () =>
                {
                    var userService = new UserService(new AppDbContext());
                    int userCount = await userService.GetUserCountAsync();
                    int apiUserCount = await getChangeCount();
                    if (userCount != apiUserCount)
                    {
                        dumpUserInformation();
                    }
                    return true;
                });
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
            
        }
        public async Task<bool> VerifyEmailInDbAsync(string email)
        {
            try
            {
                return await Task.Run(async () =>
                {
                    var userService = new UserService(new AppDbContext());

                    bool emailExists = await userService.EmailExistsAsync(email);

                    return emailExists;
                });
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<String> LoginUser(string username, string password)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var userService = new UserService(new AppDbContext());
                    return await userService.LogUserLogin(username, password); ;
                });
                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        public async Task DumpLineMastersInformationAsync()
        {
            ApiClient apiClient = new ApiClient();
            string url = "http://152.42.249.231:8000/api/method/get_linemasters";

            LineMasterResponse apiResponse = await apiClient.GetAsync<LineMasterResponse>(url);

            if (apiResponse != null && apiResponse.Status == "success")
            {
                foreach (var lineMaster in apiResponse.Data)
                {
                    System.Diagnostics.Debug.WriteLine($"Line Name: {lineMaster.LineName}");
                    System.Diagnostics.Debug.WriteLine($"Line Master: {lineMaster.lmaster}");
                }
            }
            else
            {
                Console.WriteLine("No data received or status is not 'success'.");
            }
        }
    }
}
