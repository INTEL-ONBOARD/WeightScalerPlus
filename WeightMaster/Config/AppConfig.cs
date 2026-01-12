using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

//this can be used to read config data from config.json file at the application startup
namespace WeightMaster.Config
{
    public class AppConfig
    {
        public string branchId { get; set; }
        public string branchName { get; set; }
        public string buildNo { get; set; }

        // Constructor automatically reads the json file and assigns data from the json file
        public AppConfig()
        {
            // Get the directory of the executing assembly
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //string exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            // Construct the full path to config.json
            string configPath = Path.Combine(exeDirectory, "config.json");
            // Read the JSON file content
            string jsonContent = File.ReadAllText(configPath);
            // Deserialize the JSON into a dictionary to avoid recursion issues
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
            // Assign values
            this.branchId = dict["branch_id"];
            this.branchName = dict["branch_name"];
            this.buildNo = dict["build_no"];
        }
    }
}
