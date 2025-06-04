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
using System.Windows.Documents;
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

        public async Task DumpLineMastersInformationAsync()
        {

            ApiClient apiClient = new ApiClient();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Allows case-insensitive mapping
            };
            string url = "http://152.42.249.231:8000/api/method/get_linemasters";

            LineMasterResponse apiResponse = await apiClient.PostAsync<LineMasterResponse>(url,null , options);
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
            Response apiResponse = await apiClient.PostAsync<Response>(url, null,options);

            if (apiResponse != null && apiResponse.Status == "success")
            {
                currentCloudCOunt  =  apiResponse.Data.Members.Count();
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
                await DumpMemberInformation();
                return "Unknown";
            }
        }

        

        public async Task<String> getMemberNumberId(String id)
        {
            try
            {
                var memService = new MemService(new AppDbContext());
                String data = await memService.GetCellNumberByCustomMemberNumAsync(id);
                System.Diagnostics.Debug.WriteLine(">>>>!" + data);
                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving line master data: {ex.Message}");
                await DumpMemberInformation();
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
                bool result = await UpdateGreenLeafCollectionAsync(model);
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

                                bool result = await UpdateGreenLeafCollectionAsync(model);
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

                                bool resultFinal = await UpdateBagWeightCollectionAsync(modelFinal);
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



        public async Task<List<TransactionLogBlockModel>> GetFilteredTransactionData(string lineName)
        {
            try
            {
                var transactionService = new TransactionService(new AppDbContext());
                var data = await transactionService.GetTransactionsNotInRunLogAsync(lineName);

                System.Diagnostics.Debug.WriteLine("Filtered transactions with bag_count > 0 retrieved successfully!");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data: {ex.Message}");
                return new List<TransactionLogBlockModel>();
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

        public async Task<List<TransactionLogBlockModel>> GetFilteredTransactionsByBarcodeAndDateAsync(string barcodeDetails)
        {
            try
            {
                var transactionService = new TransactionService(new AppDbContext());
                var data = await transactionService.GetTransactionsByBarcodeAndDateAsync(barcodeDetails);

                System.Diagnostics.Debug.WriteLine($"Filtered transactions forbarcode: {barcodeDetails} on today's date retrieved successfully!");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data for barcode {barcodeDetails}: {ex.Message}");
                return new List<TransactionLogBlockModel>();
            }
        }

        public async Task<List<TransactionLogBlockModel>> GetFilteredTransactionData(string barcode,string linename)
        {
            try
            {
                var transactionService = new TransactionService(new AppDbContext());
                var data = await transactionService.GetTransactionByBarcodeAndDateAsync(barcode,linename);

                System.Diagnostics.Debug.WriteLine("Filtered transactions with bag_count > 0 retrieved successfully!");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data: {ex.Message}");
                return new List<TransactionLogBlockModel>(){ };
                ;
            }
        }
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

        public async Task<bool> SetFinalTransactionAsync(FinalTransactionBlockModel model,string code)
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
                bool result = await UpdateBagWeightCollectionAsync(model);
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

        public async Task<List<FinalTransactionBlockModel>> getPrintData_2_onCustomDate(string linename,string date_)
        {
            try
            {
                var transactionService = new FinalTransactionService(new AppDbContext());
                var data = await transactionService.getDataForPrintOnCustomDate(linename,date_);

                //System.Diagnostics.Debug.WriteLine("===== Print data");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving filtered transaction data: {ex.Message}");
                return new List<FinalTransactionBlockModel>();
            }
        }

        public async Task getMemberData()
        {
            int currentCloudCount = 0;
            var service = new MemService(new AppDbContext());
            int memberCount = await service.GetMemberCountAsync();

            var apiClient = new CustomApiClient(); // Use your token-aware API client
            string url = "https://teacoopapi.codehub.lk/api/v1/members/thirdparty-members";

            // Make GET request
            MemberResponse? apiResponse = await apiClient.GetAsync<MemberResponse>(url);

            if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
            {
                currentCloudCount = apiResponse.Data.Count;
                if (currentCloudCount != memberCount)
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

        public async Task getLineDataAsync()
        {
            int currentCloudCount = 0;

            var service = new LineService(new AppDbContext());
            int localCount = await service.GetLineCountAsync();

            var apiClient = new CustomApiClient(); // Use token-aware client
            string url = "https://teacoopapi.codehub.lk/api/v1/linemaster/thirdparty-linemaster"; // Replace with actual endpoint

            LineResponse? apiResponse = await apiClient.GetAsync<LineResponse>(url);

            if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
            {
                currentCloudCount = apiResponse.Data.Count;

                if (currentCloudCount != localCount)
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

                bool isAvailable = await postStatusService.AnyPostStatusIsFalseAsync();
                if (isAvailable)
                {
                    GreenLeafPostModel? model =  await postStatusService.GetFirstGreenLeafPostWithStatusFalseAsync();
                    if (model != null) { bool isupdated = await PostGreenLeafToExternalApiAsync(model);
                        if (isupdated)
                        {
                            await postStatusService.UpdateStatusByPostIdAsync(model.Id, true);
                        }
                        else
                        {
                            await postStatusService.UpdateStatusByPostIdAsync(model.Id, false);

                        }
                    }
                }


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
                System.Diagnostics.Debug.WriteLine($"======> CLOUD SYNC STARTED!");

                bool isAvailable = await postStatusService.AnyPostStatusIsFalseAsync();
                if (isAvailable)
                {
                    GreenLeafPostModel? model = await postStatusService.GetFirstGreenLeafPostWithStatusFalseAsync();
                    if (model != null)
                    {
                        bool isupdated = await PostGreenLeafToExternalApiAsync(model);
                        if (isupdated)
                        {
                            await postStatusService.UpdateStatusByPostIdAsync(model.Id, true);
                        }
                        else
                        {
                            await postStatusService.UpdateStatusByPostIdAsync(model.Id, false);

                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"======> CLOUD SYNC FINISHED!");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"======> CLOUD SYNC FAILED!");
                return false;
            }
        }



        public async Task<GreenLeafPostModel?> getLatestPost()
        {
            try
            {
                var postService = new PostService(new AppDbContext());
                var latestPost = await postService.GetLatestPostAsync();
                System.Diagnostics.Debug.WriteLine($"> Latest post ID: {latestPost?.Id}");
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
                System.Diagnostics.Debug.WriteLine($"> Found post: {post?.Id}");
                return post;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving post by member and date: {ex.Message}");
                return null;
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

        public async Task<bool> PostGreenLeafToExternalApiAsync(GreenLeafPostModel postModel)
        {
            var client = new CustomApiClient();
            var url = "https://teacoopapi.codehub.lk/api/v1/greenleaf";

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Ensures camelCase for JSON
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            try
            {
                // Log serialized JSON for debugging
                var jsonBody = JsonSerializer.Serialize(postModel, options);
                System.Diagnostics.Debug.WriteLine($"> POST URL: {url}");
                System.Diagnostics.Debug.WriteLine($"> Request Body: {jsonBody}");

                var response = await client.PostAsync<object>(url, postModel);

                // If it gets here, it was successful (status code 2xx)
                return true;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"> HTTP Error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"> General Error: {ex.Message}");
                return false;
            }
        }


    }
}
