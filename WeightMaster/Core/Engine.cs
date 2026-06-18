using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using WeightMaster.Config;
using WeightMaster.Models;
using WeightMaster.Services;

namespace WeightMaster.Core
{
    public class Engine
    {
        //old
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
        //old
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
            try
            {
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
        public async Task<String> LoginUser(string username, string password)
        {
            try
            {
                String data = "unknown";
                await Task.Run(async () =>
                {
                    var userService = new UserService(new AppDbContext());
                    data = await userService.LogUserLogin(username, password); ;
                });
                return data;
            }
            catch (Exception ex)
            {
                return "unknown";
            }
        }

        public async Task<List<string>> GetUsernamesAsync()
        {
            try
            {
                var userService = new UserService(new AppDbContext());
                // Directly await the method without using Task.Run
                var data = await userService.GetAllUsernamesAsync();
                return data;
            }
            catch (Exception ex)
            {
                // Optionally log the exception here
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }
        //old
        public async Task DumpLineMastersInformationAsync()
        {

            ApiClient apiClient = new ApiClient();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Allows case-insensitive mapping
            };
            string url = "http://152.42.249.231:8000/api/method/get_linemasters";

            LineMasterResponse apiResponse = await apiClient.PostAsync<LineMasterResponse>(url, null, options);
            System.Diagnostics.Debug.WriteLine("> called 1");
            //System.Diagnostics.Debug.WriteLine("> res"+ apiResponse.Status.ToString());

            if (apiResponse != null && apiResponse.Message.Status == "success")
            {
                //System.Diagnostics.Debug.WriteLine("HERE >>>>> "+apiResponse.ToString());
                System.Diagnostics.Debug.WriteLine("> called 2");
                var userService = new LineMasterService(new AppDbContext());
                await userService.ReplaceLineMasterDataAsync(apiResponse.Message.Data);
                System.Diagnostics.Debug.WriteLine("> Data saved done");

                foreach (var lineMaster in apiResponse.Message.Data)
                {
                    //System.Diagnostics.Debug.WriteLine($"Line Name: {lineMaster.LineName}");
                    //System.Diagnostics.Debug.WriteLine($"Line Master: {lineMaster.LineMasterName}");
                }
                System.Diagnostics.Debug.WriteLine("> Data pulling done");

            }
            else
            {
                Console.WriteLine("No data received or status is not 'success'.");
            }
        }
        internal async Task<List<LineBlockModel>> getLineMasterData()
        {
            try
            {
                var lineService = new LineService(new AppDbContext());
                var data = await lineService.GetLineDataAsync();
                System.Diagnostics.Debug.WriteLine("Running!");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving line master data: {ex.Message}");
                return new List<LineBlockModel>();
            }
        }
        // old - don't use
        public async Task DumpMemberInformation()
        {

            int currentCloudCOunt = 0;
            var service = new MemberService(new AppDbContext());
            int memberCount = await service.GetMemberCountAsync();

            ApiClient apiClient = new ApiClient();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Allows case-insensitive mapping
            };
            string url = "http://152.42.249.231:8000/api/method/fetch_all_member_data";

            // Assuming memberModel is the model representing the API response for members
            Response apiResponse = await apiClient.PostAsync<Response>(url, null, options);

            if (apiResponse != null && apiResponse.Status == "success")
            {
                currentCloudCOunt = apiResponse.Data.Members.Count();
                var memberService = new MemberService(new AppDbContext());
                if (currentCloudCOunt != memberCount) await memberService.ReplaceMembersAsync(apiResponse.Data.Members);
            }
            else
            {
                Console.WriteLine("No data received or status is not 'success'.");
            }
        }
        public async Task<String> getMemberNameById(String id)
        {
            try
            {
                var memService = new MemService(new AppDbContext());
                String data = await memService.GetCustomNameWithInitialsAsync(id);
                System.Diagnostics.Debug.WriteLine("----------> " + data);
                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving line master data: {ex.Message}");
                //await DumpMemberInformation();
                return "Unknown";
            }
        }

        public async Task<bool> setTransaction(TransactionLogBlockModel model)
        {
            try
            {

                // var transactionService = new TransactionService(new AppDbContext());
                // bool data = await transactionService.AddTransactionAsync(model);
                // System.Diagnostics.Debug.WriteLine(":::::" + data);

                var runService = new RunService(new AppDbContext());
                RunLog RunLogs = new RunLog
                {
                    Status = true, // Set the status as true (or false)
                    Date = DateTime.Now, // Set the current date and time
                    Transaction = model,
                    FinalTransaction = null,
                    LastUpdated = DateTime.Now // Set the last updated time to now
                };
                await runService.AddRunLogAsync(RunLogs);
                // System.Diagnostics.Debug.WriteLine(":::::" + data);
                //bool result = await UpdateGreenLeafCollectionAsync(model);
                bool result = true;
                if (result)
                {
                    return true;
                }
                else
                {
                    RunLog log = await runService.GetMostRecentRunLogAsync();
                    log.Status = false;
                    await runService.UpdateRunLogAsync(log);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving line master data: {ex.Message}");
                // await DumpMemberInformation();
                return false;
            }
        }

        //old
        public async Task<bool> UpdateGreenLeafCollectionAsync(TransactionLogBlockModel transactionBlockModel)
        {
            ApiClient apiClient = new ApiClient();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // Allows case-insensitive mapping
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Prevents escaping Unicode characters
            };

            string url = "http://152.42.249.231:8000/api/method/update_green_leaf_collection";

            try
            {
                // Serialize the transactionBlockModel to JSON for viewing the body content
                var jsonBodyContent = JsonSerializer.Serialize(transactionBlockModel, options);

                // Log the URL and the body content in the debug console
                System.Diagnostics.Debug.WriteLine($"> URL: {url}");
                System.Diagnostics.Debug.WriteLine($"> Body: {jsonBodyContent}");

                // Send transactionBlockModel as the body of the POST request
                var apiResponse = await apiClient.PostAsync<object>(url, transactionBlockModel);

                // If we reach this point, the status code is 2xx, return true
                return true;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"> Error: {ex.Message}");
                return false; // Return false if the request failed (status code 4xx or 5xx)
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"> Error: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> verifyTransactionsCloud()
        {
            try
            {
                var runService = new RunService(new AppDbContext());
                int falseCount = await runService.GetCountOfStatusFalseAsync();
                System.Diagnostics.Debug.WriteLine("count >>> " + falseCount);

                while (falseCount > 0)
                {
                    try
                    {
                        // Fetch the latest run log with status false
                        RunLog que = await runService.GetLatestRunLogWithStatusFalseAsync();
                        if (que != null)
                        {
                            if (que.TransactionId.HasValue)  // Check if TransactionId is not null
                            {
                                System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ TRANSACTION RUN FOR " + que.TransactionId + " : CACHE VALIDATING ]::::::::::::");

                                var transService = new TransactionService(new AppDbContext());
                                TransactionLogBlockModel model = await transService.GetTransactionByIdAsync(que.TransactionId.Value);

                                //bool result = await UpdateGreenLeafCollectionAsync(model);
                                bool result = true;
                                if (result)
                                {
                                    await runService.UpdateRunLogStatusToTrueAsync(que);
                                }
                                else
                                {
                                    RunLog runlog = await runService.GetMostRecentRunLogAsync();
                                    runlog.Status = false;
                                    await runService.UpdateRunLogAsync(runlog);
                                    return false;
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("TransactionId is null for the current run log.");
                                // Return false or handle this case as needed
                            }
                        }
                        // Handle Final Transaction Validation
                        RunLog queFinal = await runService.GetLatestRunLogWithStatusFalseAsync();
                        if (queFinal != null)
                        {
                            if (queFinal.FinalTransactionId.HasValue)  // Check if FinalTransactionId is not null
                            {
                                System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ FINAL RUN FOR " + queFinal.FinalTransactionId + " : CACHE VALIDATING ]::::::::::::");

                                var FinalTransService = new FinalTransactionService(new AppDbContext());
                                FinalTransactionBlockModel modelFinal = await FinalTransService.GetTransactionByIdAsync(queFinal.FinalTransactionId.Value);

                                //bool resultFinal = await UpdateBagWeightCollectionAsync(modelFinal);
                                bool resultFinal = true;
                                if (resultFinal)
                                {
                                    await runService.UpdateRunLogStatusToTrueAsync(queFinal);
                                }
                                else
                                {
                                    RunLog runlog = await runService.GetMostRecentRunLogAsync();
                                    runlog.Status = false;
                                    await runService.UpdateRunLogAsync(runlog);
                                    return false;
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("FinalTransactionId is null for the current run log.");
                                // Return false or handle this case as needed
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ VALIDATIONS ERROR: " + innerEx.Message + " ]::::::::::::::::::");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"> Error: {ex.Message}");
                return false;
            }
        }






















        public async Task<List<TransactionLogBlockModel>> GetFilteredTransactionsByLineNameAndDateAsync(string lineName)
        {
            try
            {
                var transactionService = new TransactionService(new AppDbContext());
                var data = await transactionService.GetTransactionsByLineNameAndDateAsync(lineName);

                System.Diagnostics.Debug.WriteLine($"Filtered transactions for lineName: {lineName} on today's date retrieved successfully!");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data for lineName {lineName}: {ex.Message}");
                return new List<TransactionLogBlockModel>();
            }
        }

        public async Task<List<TransactionLogBlockModel>> GetCompletedFilteredTransactionsByLineNameAndDateAsync(string lineName)
        {
            try
            {
                var transactionService = new TransactionService(new AppDbContext());
                var data = await transactionService.GetCompleteTransactionsByLineNameAndDateAsync(lineName);

                System.Diagnostics.Debug.WriteLine($"Filtered transactions for lineName: {lineName} on today's date retrieved successfully!");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data for lineName {lineName}: {ex.Message}");
                return new List<TransactionLogBlockModel>();
            }
        }
        public async Task<List<TransactionLogBlockModel>> getDataForDocumentsWithDate(string lineName, string date_)
        {
            try
            {
                var transactionService = new TransactionService(new AppDbContext());
                var data = await transactionService.GetTransactionsByLineNameAndDateAsync(lineName, date_);

                //System.Diagnostics.Debug.WriteLine($"Filtered transactions for lineName: {lineName} and date: {date} on today's date retrieved successfully!");

                return data;
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data for lineName {lineName} and bardatecode {date}: {ex.Message}");
                return new List<TransactionLogBlockModel>();
            }
        }


        public async Task<List<TransactionLogBlockModel>> getDataForDocuments(string date_)
        {
            try
            {
                var transactionService = new TransactionService(new AppDbContext());
                var data = await transactionService.GetTransactionsByDateAsync(date_);

                //System.Diagnostics.Debug.WriteLine($"Filtered transactions for lineName: {lineName} and date: {date} on today's date retrieved successfully!");

                return data;
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data for lineName {lineName} and bardatecode {date}: {ex.Message}");
                return new List<TransactionLogBlockModel>();
            }
        }




        public async Task<List<TransactionLogBlockModel>> getCustomDataOut(string lineName, string date_)
        {
            try
            {
                var transactionService = new TransactionService(new AppDbContext());
                var data = await transactionService.getCustomData(lineName, date_);

                //System.Diagnostics.Debug.WriteLine($"Filtered transactions for lineName: {lineName} and date: {date} on today's date retrieved successfully!");

                return data;
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data for lineName {lineName} and bardatecode {date}: {ex.Message}");
                return new List<TransactionLogBlockModel>();
            }
        }
        public async Task<List<TransactionLogBlockModel>> GetFilteredTransactionsByLineNameBarcodeAndDateAsync(string lineName, string barcodeDetails)
        {
            try
            {
                var transactionService = new TransactionService(new AppDbContext());
                var data = await transactionService.GetTransactionsByLineNameBarcodeAndDateAsync(lineName, barcodeDetails);

                System.Diagnostics.Debug.WriteLine($"Filtered transactions for lineName: {lineName} and barcode: {barcodeDetails} on today's date retrieved successfully!");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data for lineName {lineName} and barcode {barcodeDetails}: {ex.Message}");
                return new List<TransactionLogBlockModel>();
            }
        }

        public async Task<List<TransactionLogBlockModel>> GetPendingTransactionsByLineBarcodeDateAsync(string lineName, string barcodeDetails)
        {
            try
            {
                var transactionService = new TransactionService(new AppDbContext());
                var data = await transactionService.GetPendingTransactionsByLineBarcodeDateAsync(lineName, barcodeDetails);

                System.Diagnostics.Debug.WriteLine($"Pending transactions for lineName: {lineName} and barcode: {barcodeDetails} on today's date retrieved successfully!");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data for lineName {lineName} and barcode {barcodeDetails}: {ex.Message}");
                return new List<TransactionLogBlockModel>();
            }
        }

        //old
        public async Task<bool> UpdateBagWeightCollectionAsync(FinalTransactionBlockModel finalTransactionBlockModel)
        {
            ApiClient apiClient = new ApiClient();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // Allows case-insensitive mapping
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Prevents escaping Unicode characters
            };

            string url = "http://152.42.249.231:8000/api/method/update_bag_weight_collection";

            try
            {
                // Serialize the finalTransactionBlockModel to JSON for viewing the body content
                var jsonBodyContent = JsonSerializer.Serialize(finalTransactionBlockModel, options);

                // Log the URL and the body content in the debug console
                System.Diagnostics.Debug.WriteLine($"> URL: {url}");
                System.Diagnostics.Debug.WriteLine($"> Body: {jsonBodyContent}");

                // Send finalTransactionBlockModel as the body of the POST request
                var apiResponse = await apiClient.PostAsync<object>(url, finalTransactionBlockModel);

                // If we reach this point, the status code is 2xx, return true
                return true;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"> Error: {ex.Message}");
                return false; // Return false if the request failed (status code 4xx or 5xx)
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"> Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SetFinalTransactionAsync(FinalTransactionBlockModel model, string code)
        {
            try
            {
                // var transactionService = new TransactionService(new AppDbContext());
                // bool data = await transactionService.AddTransactionAsync(model);
                // System.Diagnostics.Debug.WriteLine(":::::" + data);

                var runService = new RunService(new AppDbContext());
                TransactionService service = new TransactionService(new AppDbContext());
                int id_ = (int)await service.GetTransactionIdByBarcodeAsync(code);
                model.Id = id_;
                RunLog runLogs = new RunLog
                {
                    Status = true, // Set the status as true (or false)
                    Date = DateTime.Now, // Set the current date and time
                    Transaction = null, // No need to set Transaction here for FinalTransactionBlockModel
                    FinalTransaction = model, // Set the FinalTransaction to the model
                    LastUpdated = DateTime.Now // Set the last updated time to now
                };
                await runService.AddRunLogAsync(runLogs);

                // Call UpdateBagWeightCollectionAsync method
                //bool result = await UpdateBagWeightCollectionAsync(model);
                bool result = true;
                if (result)
                {
                    return true;
                }
                else
                {
                    RunLog log = await runService.GetMostRecentRunLogAsync();
                    log.Status = false;
                    await runService.UpdateRunLogAsync(log);
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving data: {ex.Message}");
                // await DumpMemberInformation();
                return false;
            }
        }

        public async Task<List<FinalTransactionBlockModel>> getPrintData_1()
        {
            try
            {
                var transactionService = new TransactionService(new AppDbContext());
                var data = await transactionService.getDataForPrint();

                //System.Diagnostics.Debug.WriteLine("===== Print data");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data: {ex.Message}");
                return new List<FinalTransactionBlockModel>();
            }
        }

        public async Task<List<FinalTransactionBlockModel>> getPrintData_2(string linename)
        {
            try
            {
                var transactionService = new FinalTransactionService(new AppDbContext());
                var data = await transactionService.getDataForPrint(linename);

                //System.Diagnostics.Debug.WriteLine("===== Print data");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data: {ex.Message}");
                return new List<FinalTransactionBlockModel>();
            }
        }

        public async Task<List<FinalTransactionBlockModel>> getPrintData_2_onCustomDate(string linename, string date_)
        {
            try
            {
                var transactionService = new FinalTransactionService(new AppDbContext());
                var data = await transactionService.getDataForPrintOnCustomDate(linename, date_);

                //System.Diagnostics.Debug.WriteLine("===== Print data");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data: {ex.Message}");
                return new List<FinalTransactionBlockModel>();
            }
        }

        public async Task<List<FinalTransactionBlockModel>> getTransDataByDate(string date_)
        {
            try
            {
                var transactionService = new FinalTransactionService(new AppDbContext());
                var data = await transactionService.getTransDataByDate(date_);

                //System.Diagnostics.Debug.WriteLine("===== Print data");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data: {ex.Message}");
                return new List<FinalTransactionBlockModel>();
            }
        }

        public async Task<List<TransactionLogBlockModel>> getBoxOnlyTransactions(string date_)
        {
            try
            {
                var transactionService = new TransactionService(new AppDbContext());
                var data = await transactionService.getBoxesAndPendingBagsOnly(date_);

                //System.Diagnostics.Debug.WriteLine($"Filtered transactions for lineName: {lineName} and date: {date} on today's date retrieved successfully!");

                return data;
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data for lineName {lineName} and bardatecode {date}: {ex.Message}");
                return new List<TransactionLogBlockModel>();
            }
        }

        //new
        public async Task getMemberData()
        {
            int currentCloudCount = 0;
            var service = new MemService(new AppDbContext());
            int memberCount = await service.GetMemberCountAsync();

            var apiClient = new CustomApiClient(); // Use your token-aware API client
            string url = "https://api.teacoop.lk/api/v1/members/thirdparty-members";

            // Make GET request
            MemberResponse? apiResponse = await apiClient.GetAsync<MemberResponse>(url);

            if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
            {
                currentCloudCount = apiResponse.Data.Count;
                if (currentCloudCount > memberCount)
                {
                    await service.ReplaceMembersAsync(apiResponse.Data);
                    Console.WriteLine("> Member data replaced successfully.");
                }
                else
                {
                    Console.WriteLine("> No update required. Local and cloud member counts match.");
                }
            }
            else
            {
                Console.WriteLine("No data received or response indicates failure.");
            }
        }
        //new
        public async Task getLineDataAsync(int id)
        {
            int currentCloudCount = 0;

            var service = new LineService(new AppDbContext());
            int localCount = await service.GetLineCountAsync();
            bool hasMissingLineIds = await service.HasMissingLineIdsAsync();

            var apiClient = new CustomApiClient(); // Use token-aware client
            string url = "https://api.teacoop.lk/api/v1/linemaster/thirdparty-linemaster/" + id; // Replace with actual endpoint

            LineResponse? apiResponse = await apiClient.GetAsync<LineResponse>(url);

            if (apiResponse.Success)
            {
                currentCloudCount = apiResponse.Data.Count;

                if (currentCloudCount != localCount || hasMissingLineIds)
                {
                    await service.ReplaceLineDataAsync(apiResponse.Data);
                    Console.WriteLine("> Line data replaced successfully.");
                }
                else
                {
                    Console.WriteLine("> No update required. Local and cloud line counts match.");
                }
            }
            else
            {
                Console.WriteLine("No data received or response indicates failure.");
            }
        }
        public async Task<GreenLeafPostModel?> getPostById(int id)
        {
            try
            {
                var postService = new PostService(new AppDbContext());
                var post = await postService.GetPostByIdAsync(id);
                System.Diagnostics.Debug.WriteLine("Post fetched successfully.");
                return post;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching post by ID {id}: {ex.Message}");
                return null;
            }
        }
        public async Task<bool> addPost(GreenLeafPostModel post)
        {
            try
            {
                var postService = new PostService(new AppDbContext());
                var success = await postService.AddPostAsync(post);
                System.Diagnostics.Debug.WriteLine("Post added successfully.");
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding post: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> updatePost(int id, GreenLeafPostModel post)
        {
            try
            {
                var postService = new PostService(new AppDbContext());
                var postStatusService = new PostStatusService(new AppDbContext());

                await postService.UpdatePostAsync(id, post);
                System.Diagnostics.Debug.WriteLine($"Post with ID {id} updated successfully.");

                //bool isAvailable = await postStatusService.AnyPostStatusIsFalseAsync();
                //if (isAvailable)
                //{
                //    GreenLeafPostModel? model =  await postStatusService.GetFirstGreenLeafPostWithStatusFalseAsync();
                //    if (model != null) { bool isupdated = await PostGreenLeafToExternalApiAsync(model);
                //        if (isupdated)
                //        {
                //            await postStatusService.UpdateStatusByPostIdAsync(model.Id, true);
                //        }
                //        else
                //        {
                //            await postStatusService.UpdateStatusByPostIdAsync(model.Id, false);

                //        }
                //    }
                //}


                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating post with ID {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> cloudSync()
        {
            try
            {
                var postStatusService = new PostStatusService(new AppDbContext());
                System.Diagnostics.Debug.WriteLine("======> CLOUD SYNC STARTED!");
                bool isAvailable = true;
                do
                {
                    isAvailable = await postStatusService.AnyPostStatusIsFalseAsync();
                    //MessageBox.Show("===> [checking] " + isAvailable);
                    if (isAvailable)
                    {
                        GreenLeafPostModel? model = await postStatusService.GetFirstGreenLeafPostWithStatusFalseAsync();
                        //MessageBox.Show("===> [fetched] " + model?.id);
                        if (model != null)
                        {
                            bool isupdated = await PostGreenLeafToExternalApiAsync(model);
                            if (isupdated)
                            {
                                await postStatusService.UpdateStatusByPostIdAsync(model.id, true);
                                //MessageBox.Show("===>[done] " + model.id);
                            }
                            else
                            {
                                // If sync fails, stop the loop to prevent infinite retries of the same record
                                // The record remains Status=false and will be retried next time cloudSync is called
                                System.Diagnostics.Debug.WriteLine($"======> Sync failed for Post ID: {model.id}. Stopping sync batch.");
                                return false; 
                            }
                        }
                    }
                    // Loop will continue only if there are more items AND the last one was successful
                } while (isAvailable);
                System.Diagnostics.Debug.WriteLine("======> CLOUD SYNC FINISHED!");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("======> CLOUD SYNC FAILED!");
                return false;
            }
        }

        public async Task<GreenLeafPostModel?> getLatestPost()
        {
            try
            {
                var postService = new PostService(new AppDbContext());
                var latestPost = await postService.GetLatestPostAsync();
                System.Diagnostics.Debug.WriteLine($"> Latest post ID: {latestPost?.id}");
                return latestPost;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving latest post: {ex.Message}");
                return null;
            }
        }
        public async Task<List<GreenLeafPostModel>> getPostsByMemberAndDate(string memberNumber, string date)
        {
            try
            {
                var postService = new PostService(new AppDbContext());
                var posts = await postService.GetPostsByMemberAndDateAsync(memberNumber, date);
                System.Diagnostics.Debug.WriteLine($"> Found {posts.Count} post(s)");
                return posts;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving posts by member and date: {ex.Message}");
                return new List<GreenLeafPostModel>();
            }
        }

        public async Task<GreenLeafPostModel?> getPostByMemberAndDate(string memberNumber, string date)
        {
            try
            {
                var postService = new PostService(new AppDbContext());
                var post = await postService.GetPostByMemberAndDateAsyncSingle(memberNumber, date);
                System.Diagnostics.Debug.WriteLine($"> Found post: {post?.id}");
                return post;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving post by member and date: {ex.Message}");
                return null;
            }
        }

        // Line-aware variant: returns the row for the specific line being weighed, so Station 2 updates
        // the right greenleafpost instead of always hitting the first one (which renamed lines / left 0s).
        public async Task<GreenLeafPostModel?> getPostByMemberDateAndLine(string memberNumber, string date, string line)
        {
            try
            {
                var postService = new PostService(new AppDbContext());
                var post = await postService.GetPostByMemberDateAndLineAsyncSingle(memberNumber, date, line);
                System.Diagnostics.Debug.WriteLine($"> Found post (line-aware): {post?.id}");
                return post;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving post by member/date/line: {ex.Message}");
                return null;
            }
        }

        public async Task<List<GreenLeafPostModel>> GetAllPostsByFilteringPost(string memberNumber, string line)
        {
            try
            {
                var postService = new PostService(new AppDbContext());
                var posts = await postService.GetAllPostsByFiltering(memberNumber, line);
                System.Diagnostics.Debug.WriteLine($"> Found {posts.Count} post(s)");
                return posts;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving posts by member and date: {ex.Message}");
                return new List<GreenLeafPostModel>();
            }
        }

        public async Task<memDbLog?> GetMemberByCustomMemberNumAsync(string customMemberNum)
        {
            try
            {
                var memService = new MemService(new AppDbContext());
                var member = await memService.GetMemberByCustomMemberNumAsync(customMemberNum);
                System.Diagnostics.Debug.WriteLine($"> Found member: {member?.CustomMemberNum} - {member?.CustomNameWithInitials}");
                return member;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving member by CustomMemberNum: {ex.Message}");
                return null;
            }
        }
        //new
        public async Task<bool> PostGreenLeafToExternalApiAsync(GreenLeafPostModel postModel)
        {
            var client = new CustomApiClient();
            var url = "https://api.teacoop.lk/api/v1/greenleaf";
            await PopulateLineIdIfMissingAsync(postModel);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            try
            {
                // Serialize the request body for logging
                var jsonBody = JsonSerializer.Serialize(postModel, options);
                System.Diagnostics.Debug.WriteLine($"> POST URL: {url}");
                System.Diagnostics.Debug.WriteLine($"> Request Body: {jsonBody}");
                //MessageBox.Show(jsonBody);
                // Make the API call
                var response = await client.PostAsync<object>(url, postModel);

                // Log success
                System.Diagnostics.Debug.WriteLine($"> POST succeeded to: {url}");
                return true;
            }
            catch (HttpRequestException ex)
            {
                 // Check for 409 Conflict (Duplicate) - Treat as Success
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    System.Diagnostics.Debug.WriteLine("> Duplicate record found (409). Treating as success.");
                    return true;
                }

                System.Diagnostics.Debug.WriteLine($"> HTTP Error: {ex.Message}");

                if (ex.Data != null)
                {
                    foreach (var key in ex.Data.Keys)
                    {
                        System.Diagnostics.Debug.WriteLine($"> Extra HTTP Error Info: {key}: {ex.Data[key]}");
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"> General Error: {ex.Message}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"> Inner Exception: {ex.InnerException.Message}");
                }

                return false;
            }
        }

        private async Task PopulateLineIdIfMissingAsync(GreenLeafPostModel postModel)
        {
            if (!string.IsNullOrWhiteSpace(postModel.line_id) || string.IsNullOrWhiteSpace(postModel.transportlinename))
            {
                return;
            }

            try
            {
                var lineService = new LineService(new AppDbContext());
                postModel.line_id = await lineService.GetLineIdByLineNameAsync(postModel.transportlinename);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"> Unable to resolve line_id for greenleaf post {postModel.id}: {ex.Message}");
            }
        }

        // get all transport bills
        public async Task<List<TransportBillBlockModel>> getAllTransportBillsAsync()
        {
            try
            {
                var transportBillService = new TransportBillService(new AppDbContext());
                var bills = await transportBillService.GetAllTransportBillsAsync();
                System.Diagnostics.Debug.WriteLine($"> Found {bills.Count} transport bill(s)");
                return bills;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving transport bills: {ex.Message}");
                return new List<TransportBillBlockModel>();
            }
        }

        // add transport bill
        public async Task<bool> addTransportBillAsync(TransportBillBlockModel bill)
        {
            try
            {
                var transportBillService = new TransportBillService(new AppDbContext());
                // Check for existing bill with same BillNumber
                bool exists = await transportBillService.TransportBillExistsAsync(bill.billNo);
                if (exists)
                {
                    System.Diagnostics.Debug.WriteLine("Transport bill with this Bill Number already exists.");
                    return true;
                }
                var success = await transportBillService.SaveTransportBillAsync(bill);
                System.Diagnostics.Debug.WriteLine("Transport bill added successfully.");
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding transport bill: {ex.Message}");
                return false;
            }
        }

        //get transport bill by date
        public async Task<List<TransportBillBlockModel>> getTransportBillsByDateAsync(string date)
        {
            try
            {
                var transportBillService = new TransportBillService(new AppDbContext());
                var bills = await transportBillService.GetTransportBillsByDateAsync(date);
                System.Diagnostics.Debug.WriteLine($"> Found {bills.Count} transport bill(s) for date: {date}");
                return bills;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving transport bills by date: {ex.Message}");
                return new List<TransportBillBlockModel>();
            }
        }

        public async Task<string?> GetTransportBillNumberByLineNameAsync(string lineName, string date = null)
        {
            date ??= DateTime.Today.ToString("yyyy-MM-dd");
            try
            {
                using var context = new AppDbContext();
                var transportBillService = new TransportBillService(context);
                var billNo = await transportBillService.GetTransportBillNoByLineNameAsync(lineName, date); // single string?
                System.Diagnostics.Debug.WriteLine($"> Found {(billNo != null ? 1 : 0)} bill number(s) for line: {lineName}");
                return billNo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving transport bill number by line name: {ex.Message}");
                return null;
            }
        }



    }
}
