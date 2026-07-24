using CodeNect_Website.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CodeNect_Website.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _config;

        public HomeController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            int totalAccounts = 0;

            if (!string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    using var conn = new MySqlConnection(connectionString);
                    conn.Open();
                    const string sqlCount = "SELECT COUNT(`ACCOUNT_ID`) FROM `account`";
                    using var cmdCount = new MySqlCommand(sqlCount, conn);
                    var result = cmdCount.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        totalAccounts = Convert.ToInt32(result);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error counting accounts: " + ex.Message);
                }
            }

            ViewData["TotalAccounts"] = totalAccounts;
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginModel model)
        {
            string connStr = _config.GetConnectionString("DefaultConnection") ?? "";

            try
            {
                using var conn = new MySqlConnection(connStr);
                conn.Open();

                var cmd = new MySqlCommand("SELECT `ACCOUNT_ID` FROM `account` WHERE `USER_NAME` = @u AND `PASSWORD` = @p", conn);
                cmd.Parameters.AddWithValue("@u", model.Username);
                cmd.Parameters.AddWithValue("@p", model.Password);

                var id = cmd.ExecuteScalar();

                if (id != null)
                {
                    string accountId = id.ToString() ?? string.Empty;
                    HttpContext.Session.SetString("LoggedInAccountId", accountId);
                    return RedirectToAction("Dashboard");
                }

                ViewBag.Error = "Invalid username or password.";
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
            }

            return View("Index");
        }

        [HttpGet] public IActionResult Register() => View();

        [HttpPost]
        public IActionResult RegisterSubmit(RegisterModel model)
        {
            if (!ModelState.IsValid) { ViewBag.Error = "Please fill all required fields correctly."; return View("Register", model); }
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            if (string.IsNullOrEmpty(connectionString)) { ViewBag.Error = "Database connection error."; return View("Register", model); }
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                const string checkSql = "SELECT COUNT(*) FROM `account` WHERE `USER_NAME` = @USERNAME OR `EMAIL` = @EMAIL";
                using var checkCmd = new MySqlCommand(checkSql, conn);
                checkCmd.Parameters.AddWithValue("@USERNAME", model.USERNAME);
                checkCmd.Parameters.AddWithValue("@EMAIL", model.EMAIL);
                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0) { ViewBag.Error = "Username or Email already exists."; return View("Register", model); }

                string newAccountId = "ACC-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                const string insertSql = @"INSERT INTO `account` 
                (`ACCOUNT_ID`, `ACCOUNT`, `ADDRESS`, `CONTACT`, `EMAIL`, `USER_NAME`, `PASSWORD`, `SERIAL_NUMBER`, `STATUS`, `CREATE_AT`) 
                VALUES (@AccountId, @Account, @Address, @Contact, @Email, @UserName, @Password, @Serial, 'OFFLINE', NOW())";

                using var cmd = new MySqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("@AccountId", newAccountId);
                cmd.Parameters.AddWithValue("@Account", model.ACCOUNT);
                cmd.Parameters.AddWithValue("@Address", string.IsNullOrWhiteSpace(model.ADDRESS) ? DBNull.Value : model.ADDRESS);
                cmd.Parameters.AddWithValue("@Contact", model.CONTACT);
                cmd.Parameters.AddWithValue("@Email", model.EMAIL);
                cmd.Parameters.AddWithValue("@UserName", model.USERNAME);
                cmd.Parameters.AddWithValue("@Password", model.PASSWORD);
                cmd.Parameters.AddWithValue("@Serial", string.IsNullOrWhiteSpace(model.SERIAL_NUMBER) ? DBNull.Value : model.SERIAL_NUMBER);
                cmd.ExecuteNonQuery();

                ViewBag.Success = "Account created successfully! You can now log in.";
                return View("Index");
            }
            catch (Exception ex) { ViewBag.Error = "Registration failed: " + ex.Message; return View("Register", model); }
        }

        public IActionResult Dashboard()
        {
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId)) return RedirectToAction("Index");
            int branchCount = 0, userCount = 0;
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                var cmdBranches = new MySqlCommand("SELECT COUNT(*) FROM `branches` WHERE `ACCOUNT_ID` = @AccountId", conn);
                cmdBranches.Parameters.AddWithValue("@AccountId", accountId);
                branchCount = Convert.ToInt32(cmdBranches.ExecuteScalar());
                var cmdUsers = new MySqlCommand("SELECT COUNT(*) FROM `user_accounts` WHERE `ACCOUNT` = @AccountId", conn);
                cmdUsers.Parameters.AddWithValue("@AccountId", accountId);
                userCount = Convert.ToInt32(cmdUsers.ExecuteScalar());
            }
            catch (Exception ex) { ViewBag.Error = "Error loading data: " + ex.Message; }
            ViewBag.BranchCount = branchCount; ViewBag.UserCount = userCount;
            return View();
        }

        public IActionResult Branches()
        {
            var branches = new List<BranchModel>();
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId)) return RedirectToAction("Index");
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                const string sql = @"SELECT `ID`, `BRANCH_ID`, `BRANCH`, `ADDRESS`, `BUSINESS_TYPE`, `CONTACT`, `EMAIL`, `MANAGER`, `REGISTRATION_DATE`, `STATUS`, `TIN`, `TIN_REGISTERED` 
                                    FROM `branches` WHERE `ACCOUNT_ID` = @AccountId ORDER BY `BRANCH` ASC";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    branches.Add(new BranchModel
                    {
                        ID = reader.GetInt32("ID"),
                        BRANCH_ID = reader["BRANCH_ID"]?.ToString() ?? string.Empty,
                        BRANCH = reader["BRANCH"]?.ToString() ?? string.Empty,
                        ADDRESS = reader["ADDRESS"]?.ToString() ?? string.Empty,
                        BUSINESS_TYPE = reader["BUSINESS_TYPE"]?.ToString() ?? string.Empty,
                        CONTACT = reader["CONTACT"]?.ToString() ?? string.Empty,
                        EMAIL = reader["EMAIL"]?.ToString() ?? string.Empty,
                        MANAGER = reader["MANAGER"]?.ToString() ?? string.Empty,
                        REGISTRATION_DATE = reader.IsDBNull("REGISTRATION_DATE") ? (DateTime?)null : reader.GetDateTime("REGISTRATION_DATE"),
                        STATUS = reader["STATUS"]?.ToString() ?? "Active",
                        TIN = reader["TIN"]?.ToString() ?? string.Empty,
                        TIN_REGISTERED = reader.IsDBNull("TIN_REGISTERED") ? (DateTime?)null : reader.GetDateTime("TIN_REGISTERED")
                    });
                }
            }
            catch (Exception ex) { ViewBag.Error = "Unable to load branches: " + ex.Message; }
            ViewBag.Branches = branches;
            return View();
        }

        public IActionResult CreateBranch()
        {
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId)) return RedirectToAction("Index");
            ViewBag.Managers = GetManagersList(accountId);
            return View();
        }

        [HttpPost]
        public IActionResult CreateBranchSubmit()
        {
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId)) { ViewBag.Error = "Please log in first."; ViewBag.Managers = GetManagersList(""); return View("CreateBranch"); }
            string branchName = Request.Form["BRANCH"].ToString() ?? "";
            string businessType = Request.Form["BUSINESS_TYPE"].ToString() ?? "";
            string tin = Request.Form["TIN"].ToString() ?? "";
            string tinRegistered = Request.Form["TIN_REGISTERED"].ToString() ?? "";
            string address = Request.Form["ADDRESS"].ToString() ?? "";
            string email = Request.Form["EMAIL"].ToString() ?? "";
            string contact = Request.Form["CONTACT"].ToString() ?? "";
            string manager = Request.Form["MANAGER"].ToString() ?? "";
            const string status = "Active";
            DateTime registrationDate = DateTime.Now;

            string newBranchId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            if (string.IsNullOrWhiteSpace(businessType)) { ViewBag.Error = "Please fill required field: Business Type."; ViewBag.Managers = GetManagersList(accountId); return View("CreateBranch"); }
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                const string insertSql = @"INSERT INTO `branches` 
                (`ACCOUNT_ID`, `BRANCH_ID`, `BRANCH`, `TIN`, `TIN_REGISTERED`, `BUSINESS_TYPE`, `ADDRESS`, `EMAIL`, `CONTACT`, `MANAGER`, `REGISTRATION_DATE`, `STATUS`) 
                VALUES (@AccountId, @BranchId, @Branch, @Tin, @TinRegistered, @BusinessType, @Address, @Email, @Contact, @Manager, @RegDate, @Status)";
                using var cmd = new MySqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                cmd.Parameters.AddWithValue("@BranchId", newBranchId);
                cmd.Parameters.AddWithValue("@Branch", string.IsNullOrWhiteSpace(branchName) ? DBNull.Value : branchName);
                cmd.Parameters.AddWithValue("@Tin", string.IsNullOrWhiteSpace(tin) ? DBNull.Value : tin);
                cmd.Parameters.AddWithValue("@TinRegistered", string.IsNullOrWhiteSpace(tinRegistered) ? DBNull.Value : tinRegistered);
                cmd.Parameters.AddWithValue("@BusinessType", businessType);
                cmd.Parameters.AddWithValue("@Address", string.IsNullOrWhiteSpace(address) ? DBNull.Value : address);
                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email);
                cmd.Parameters.AddWithValue("@Contact", string.IsNullOrWhiteSpace(contact) ? DBNull.Value : contact);
                cmd.Parameters.AddWithValue("@Manager", string.IsNullOrWhiteSpace(manager) ? DBNull.Value : manager);
                cmd.Parameters.AddWithValue("@RegDate", registrationDate);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.ExecuteNonQuery();
                return RedirectToAction("Branches");
            }
            catch (Exception ex) { ViewBag.Error = "Error: " + ex.Message; ViewBag.Managers = GetManagersList(accountId); return View("CreateBranch"); }
        }

        public IActionResult UserAccount()
        {
            var users = new List<UserAccountModel>();
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId)) return RedirectToAction("Index");
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                const string sql = @"SELECT u.`ID`, u.`ACCOUNT`, u.`BRANCH_ID`, b.`BRANCH`, u.`FULL_NAME`, u.`USERNAME`, u.`EMAIL`, u.`CONTACT`, u.`USER_TYPE`, u.`STATUS`, u.`DATE_CREATED`, u.`LAST_LOGIN_DATETIME` 
                                    FROM `user_accounts` u
                                    LEFT JOIN `branches` b ON u.`BRANCH_ID` = b.`BRANCH_ID` AND u.`ACCOUNT` = b.`ACCOUNT_ID`
                                    WHERE u.`ACCOUNT` = @AccountId ORDER BY u.`FULL_NAME` ASC";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    users.Add(new UserAccountModel
                    {
                        ID = reader.GetInt32("ID"),
                        ACCOUNT = reader["ACCOUNT"]?.ToString() ?? string.Empty,
                        BRANCH_ID = reader["BRANCH_ID"]?.ToString() ?? "—",
                        BRANCH = reader["BRANCH"]?.ToString() ?? "—",
                        FULL_NAME = reader["FULL_NAME"]?.ToString() ?? string.Empty,
                        USERNAME = reader["USERNAME"]?.ToString() ?? string.Empty,
                        EMAIL = reader.IsDBNull("EMAIL") ? null : reader["EMAIL"]?.ToString(),
                        CONTACT = reader.IsDBNull("CONTACT") ? null : reader["CONTACT"]?.ToString(),
                        USER_TYPE = reader["USER_TYPE"]?.ToString() ?? "Staff",
                        STATUS = reader["STATUS"]?.ToString() ?? "Active",
                        DATE_CREATED = reader.GetDateTime("DATE_CREATED"),
                        LAST_LOGIN_DATETIME = reader.IsDBNull("LAST_LOGIN_DATETIME") ? null : reader.GetDateTime("LAST_LOGIN_DATETIME")
                    });
                }
            }
            catch (Exception ex) { ViewBag.Error = "Unable to load users: " + ex.Message; }
            ViewBag.Users = users;
            return View();
        }

        public IActionResult CreateUser()
        {
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId)) return RedirectToAction("Index");
            ViewBag.Branches = GetBranchesDropdown(accountId);
            return View();
        }

        [HttpPost]
        public IActionResult CreateUserSubmit()
        {
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId)) { ViewBag.Error = "Please log in first."; ViewBag.Branches = GetBranchesDropdown(""); return View("CreateUser"); }
            string branchId = Request.Form["BRANCH_ID"].ToString() ?? "";
            string username = Request.Form["USERNAME"].ToString() ?? "";
            string password = Request.Form["PASSWORD"].ToString() ?? "";
            string fullName = Request.Form["FULL_NAME"].ToString() ?? "";
            string userType = Request.Form["USER_TYPE"].ToString() ?? "Staff";
            string status = Request.Form["STATUS"].ToString() ?? "Active";
            string email = Request.Form["EMAIL"].ToString() ?? "";
            string contact = Request.Form["CONTACT"].ToString() ?? "";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName))
            {
                ViewBag.Error = "Please fill all required information.";
                ViewBag.Branches = GetBranchesDropdown(accountId);
                return View("CreateUser");
            }

            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                const string insertSql = @"INSERT INTO `user_accounts` 
                (`ACCOUNT`, `BRANCH_ID`, `FULL_NAME`, `USERNAME`, `PASSWORD`, `EMAIL`, `CONTACT`, `USER_TYPE`, `STATUS`, `DATE_CREATED`) 
                VALUES (@Account, @BranchId, @FullName, @Username, @Password, @Email, @Contact, @UserType, @Status, NOW())";
                using var cmd = new MySqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("@Account", accountId);
                cmd.Parameters.AddWithValue("@BranchId", string.IsNullOrWhiteSpace(branchId) ? DBNull.Value : branchId);
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);
                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email);
                cmd.Parameters.AddWithValue("@Contact", string.IsNullOrWhiteSpace(contact) ? DBNull.Value : contact);
                cmd.Parameters.AddWithValue("@UserType", userType);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.ExecuteNonQuery();

                if (userType.Equals("Manager", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(branchId))
                {
                    const string updateSql = @"UPDATE `branches` SET `MANAGER` = @ManagerName 
                                                WHERE `BRANCH_ID` = @BranchId AND `ACCOUNT_ID` = @AccountId";
                    using var cmdUpdate = new MySqlCommand(updateSql, conn);
                    cmdUpdate.Parameters.AddWithValue("@ManagerName", fullName);
                    cmdUpdate.Parameters.AddWithValue("@BranchId", branchId);
                    cmdUpdate.Parameters.AddWithValue("@AccountId", accountId);
                    cmdUpdate.ExecuteNonQuery();
                }

                return RedirectToAction("UserAccount");
            }
            catch (Exception ex) { ViewBag.Error = "Error: " + ex.Message; ViewBag.Branches = GetBranchesDropdown(accountId); return View("CreateUser"); }
        }

        private List<string> GetManagersList(string accountId)
        {
            var list = new List<string> { "-- Select Manager --" };
            if (string.IsNullOrEmpty(accountId)) return list;
            try
            {
                using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();
                const string sql = "SELECT `FULL_NAME` FROM `user_accounts` WHERE `ACCOUNT` = @AccountId AND `USER_TYPE` = 'Manager' AND `STATUS` = 'Active'";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add(reader["FULL_NAME"]?.ToString() ?? string.Empty);
            }
            catch { }
            return list;
        }

        private List<dynamic> GetBranchesDropdown(string accountId)
        {
            var list = new List<dynamic> { new { Id = "", Name = "-- Select Branch --" } };
            if (string.IsNullOrEmpty(accountId)) return list;
            try
            {
                using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();
                const string sql = "SELECT `BRANCH_ID`, `BRANCH` FROM `branches` WHERE `ACCOUNT_ID` = @AccountId ORDER BY `BRANCH`";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add(new { Id = reader["BRANCH_ID"]?.ToString() ?? string.Empty, Name = reader["BRANCH"]?.ToString() ?? string.Empty });
            }
            catch { }
            return list;
        }

        public IActionResult Inventory(string? branchId = "", string? searchProduct = "")
        {
            var products = new List<InventoryModel>();
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId)) return RedirectToAction("Index");

            ViewBag.Branches = GetBranchesDropdown(accountId);
            ViewBag.SelectedBranch = branchId;
            ViewBag.SearchProduct = searchProduct;

            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();

                string sql = @"SELECT i.*, b.`BRANCH` AS BRANCH_NAME
                       FROM `inventory_master_file` i
                       LEFT JOIN `branches` b 
                         ON i.`BRANCH_ID` = b.`BRANCH_ID` 
                         AND i.`ACCOUNT_ID` = b.`ACCOUNT_ID`
                       WHERE i.`ACCOUNT_ID` = @AccountId";

                if (!string.IsNullOrWhiteSpace(branchId))
                    sql += " AND i.`BRANCH_ID` = @BranchId";

                if (!string.IsNullOrWhiteSpace(searchProduct))
                    sql += " AND (i.`BRAND` LIKE @Search OR i.`BARCODE` LIKE @Search OR i.`SKU` LIKE @Search)";

                sql += " ORDER BY b.`BRANCH` ASC, i.`BRAND` ASC";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);

                if (!string.IsNullOrWhiteSpace(branchId))
                    cmd.Parameters.AddWithValue("@BranchId", branchId);

                if (!string.IsNullOrWhiteSpace(searchProduct))
                    cmd.Parameters.AddWithValue("@Search", $"%{searchProduct}%");

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    products.Add(new InventoryModel
                    {
                        ID = reader.GetInt32("ID"),
                        ACCOUNT_ID = reader["ACCOUNT_ID"]?.ToString(),
                        BRANCH_ID = reader["BRANCH_ID"]?.ToString() ?? "—",
                        BRANCH_NAME = reader["BRANCH_NAME"]?.ToString() ?? "—",
                        BARCODE = reader["BARCODE"]?.ToString(),
                        BRAND = reader["BRAND"]?.ToString(),
                        CATEGORY = reader["CATERY"]?.ToString(),
                        SIZE = reader["SIZE"]?.ToString(),
                        UNIT = reader["UNIT"]?.ToString(),
                        AVAILABLE = reader.IsDBNull("AVAILABLE") ? null : reader.GetInt32("AVAILABLE"),
                        PRICE = reader.IsDBNull("PRICE") ? null : reader.GetDecimal("PRICE"),
                        SKU = reader["SKU"]?.ToString(),
                        VENDOR = reader["VENDOR"]?.ToString(),
                        DATE_ADDED = reader.IsDBNull("DATE_ADDED") ? null : reader.GetDateTime("DATE_ADDED")
                    });
                }
            }
            catch (Exception ex) { ViewBag.Error = "Error loading inventory: " + ex.Message; }

            ViewBag.Products = products;
            return View();
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId)) return RedirectToAction("Index");
            ViewBag.Branches = GetBranchesDropdown(accountId);
            return View();
        }

        [HttpPost]
        public IActionResult AddProductSubmit()
        {
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId)) { ViewBag.Error = "Please log in first."; ViewBag.Branches = GetBranchesDropdown(""); return View("AddProduct"); }

            string branchId = Request.Form["BRANCH_ID"].ToString() ?? "";
            string barcode = Request.Form["BARCODE"].ToString() ?? "";
            string brand = Request.Form["BRAND"].ToString() ?? "";
            string category = Request.Form["CATEGORY"].ToString() ?? "";
            string size = Request.Form["SIZE"].ToString() ?? "";
            string unit = Request.Form["UNIT"].ToString() ?? "";
            string sku = Request.Form["SKU"].ToString() ?? "";
            string vendor = Request.Form["VENDOR"].ToString() ?? "";
            int available = int.TryParse(Request.Form["AVAILABLE"], out var a) ? a : 0;
            decimal price = decimal.TryParse(Request.Form["PRICE"], out var p) ? p : 0;

            if (string.IsNullOrWhiteSpace(branchId) || string.IsNullOrWhiteSpace(brand))
            {
                ViewBag.Error = "Please select a branch and enter product name/brand.";
                ViewBag.Branches = GetBranchesDropdown(accountId);
                return View("AddProduct");
            }

            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();
                const string sql = @"INSERT INTO `inventory_master_file` 
                            (`ACCOUNT_ID`, `BRANCH_ID`, `BARCODE`, `BRAND`, `CATERY`, `SIZE`, `UNIT`, `AVAILABLE`, `PRICE`, `SKU`, `VENDOR`, `DATE_ADDED`)
                            VALUES (@AccountId, @BranchId, @Barcode, @Brand, @Category, @Size, @Unit, @Available, @Price, @Sku, @Vendor, NOW())";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                cmd.Parameters.AddWithValue("@BranchId", branchId);
                cmd.Parameters.AddWithValue("@Barcode", string.IsNullOrWhiteSpace(barcode) ? DBNull.Value : barcode);
                cmd.Parameters.AddWithValue("@Brand", brand);
                cmd.Parameters.AddWithValue("@Category", string.IsNullOrWhiteSpace(category) ? DBNull.Value : category);
                cmd.Parameters.AddWithValue("@Size", string.IsNullOrWhiteSpace(size) ? DBNull.Value : size);
                cmd.Parameters.AddWithValue("@Unit", string.IsNullOrWhiteSpace(unit) ? DBNull.Value : unit);
                cmd.Parameters.AddWithValue("@Available", available);
                cmd.Parameters.AddWithValue("@Price", price);
                cmd.Parameters.AddWithValue("@Sku", string.IsNullOrWhiteSpace(sku) ? DBNull.Value : sku);
                cmd.Parameters.AddWithValue("@Vendor", string.IsNullOrWhiteSpace(vendor) ? DBNull.Value : vendor);
                cmd.ExecuteNonQuery();

                return RedirectToAction("Inventory");
            }
            catch (Exception ex) { ViewBag.Error = "Error saving product: " + ex.Message; ViewBag.Branches = GetBranchesDropdown(accountId); return View("AddProduct"); }
        }

        public IActionResult PricingPlans()
        {
            return View();
        }

        public IActionResult Logout() { HttpContext.Session.Clear(); return RedirectToAction("Index"); }
    }
}