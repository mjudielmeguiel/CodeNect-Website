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

        // ===================== LOGIN =====================
        [HttpPost]
        public IActionResult Login(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View("Index");

            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            if (string.IsNullOrEmpty(connectionString))
            {
                ViewBag.Error = "Database connection error.";
                return View("Index");
            }

            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();

                const string sql = "SELECT ACCOUNT_ID FROM account WHERE USERNAME = @Username AND PASSWORD = @Password AND STATUS = 'ACTIVE'";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Username", model.Username ?? string.Empty);
                cmd.Parameters.AddWithValue("@Password", model.Password ?? string.Empty);

                var accountId = cmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrEmpty(accountId))
                {
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

        public IActionResult Index() => View();

        // ===================== UPDATED DASHBOARD WITH COUNTS =====================
        public IActionResult Dashboard()
        {
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");

            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction("Index");

            int branchCount = 0;
            int userCount = 0;
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;

            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();

                // Bilang ng Branches ng account na ito
                const string sqlBranches = "SELECT COUNT(*) FROM branches WHERE ACCOUNT_ID = @AccountId";
                using var cmdBranches = new MySqlCommand(sqlBranches, conn);
                cmdBranches.Parameters.AddWithValue("@AccountId", accountId);
                branchCount = Convert.ToInt32(cmdBranches.ExecuteScalar());

                // Bilang ng Users ng account na ito
                const string sqlUsers = "SELECT COUNT(*) FROM user_accounts WHERE ACCOUNT_ID = @AccountId";
                using var cmdUsers = new MySqlCommand(sqlUsers, conn);
                cmdUsers.Parameters.AddWithValue("@AccountId", accountId);
                userCount = Convert.ToInt32(cmdUsers.ExecuteScalar());
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading data: " + ex.Message;
            }

            ViewBag.BranchCount = branchCount;
            ViewBag.UserCount = userCount;

            return View();
        }

        // ===================== BRANCHES LIST =====================
        public IActionResult Branches()
        {
            var branches = new List<BranchModel>();
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");

            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction("Index");

            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;

            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();

                const string sql = @"SELECT ID, BRANCH_ID, BRANCH, ADDRESS, BUSINESS_TYPE, CONTACT, EMAIL, 
                                            MANAGER, REGISTRATION_DATE, STATUS, TIN, TIN_REGISTERED
                                     FROM branches 
                                     WHERE ACCOUNT_ID = @AccountId
                                     ORDER BY BRANCH ASC";

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
            catch (Exception ex)
            {
                ViewBag.Error = "Unable to load branches: " + ex.Message;
            }

            ViewBag.Branches = branches;
            return View();
        }

        // ===================== CREATE BRANCH =====================
        public IActionResult CreateBranch()
        {
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction("Index");

            ViewBag.Managers = GetManagersList(accountId);
            return View();
        }

        [HttpPost]
        public IActionResult CreateBranchSubmit()
        {
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId))
            {
                ViewBag.Error = "Please log in first.";
                ViewBag.Managers = GetManagersList("");
                return View("CreateBranch");
            }

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

            if (string.IsNullOrWhiteSpace(businessType))
            {
                ViewBag.Error = "Please fill required field: Business Type.";
                ViewBag.Managers = GetManagersList(accountId);
                return View("CreateBranch");
            }

            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;

            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();

                const string insertSql = @"INSERT INTO `branches` 
                    (`ACCOUNT_ID`, `BRANCH`, `TIN`, `TIN_REGISTERED`, 
                     `BUSINESS_TYPE`, `ADDRESS`, `EMAIL`, `CONTACT`, `MANAGER`, 
                     `REGISTRATION_DATE`, `STATUS`)
                    VALUES 
                    (@AccountId, @Branch, @Tin, @TinRegistered,
                     @BusinessType, @Address, @Email, @Contact, @Manager,
                     @RegDate, @Status)";

                using var cmd = new MySqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);
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
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
                ViewBag.Managers = GetManagersList(accountId);
                return View("CreateBranch");
            }
        }

        // ===================== USER LIST =====================
        public IActionResult UserAccount()
        {
            var users = new List<UserAccountModel>();
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");

            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction("Index");

            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;

            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();

                const string sql = @"SELECT ID, ACCOUNT_ID, BRANCH_ID, FULL_NAME, USERNAME, EMAIL, CONTACT, 
                                            USER_TYPE, STATUS, DATE_CREATED, LAST_LOGIN_DATETIME 
                                     FROM user_accounts 
                                     WHERE ACCOUNT_ID = @AccountId
                                     ORDER BY FULL_NAME ASC";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new UserAccountModel
                    {
                        ID = reader.GetInt32("ID"),
                        ACCOUNT_ID = reader["ACCOUNT_ID"]?.ToString() ?? string.Empty,
                        BRANCH_ID = reader.IsDBNull("BRANCH_ID") ? null : reader["BRANCH_ID"]?.ToString(),
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
            catch
            {
                ViewBag.Error = "Unable to load users.";
            }

            ViewBag.Users = users;
            return View();
        }

        // ===================== ADD USER =====================
        public IActionResult CreateUser()
        {
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction("Index");

            ViewBag.Branches = GetBranchesDropdown(accountId);
            return View();
        }

        [HttpPost]
        public IActionResult CreateUserSubmit()
        {
            string? accountId = HttpContext.Session.GetString("LoggedInAccountId");
            if (string.IsNullOrEmpty(accountId))
            {
                ViewBag.Error = "Please log in first.";
                ViewBag.Branches = GetBranchesDropdown("");
                return View("CreateUser");
            }

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

                const string insertSql = @"INSERT INTO user_accounts 
                                    (ACCOUNT_ID, BRANCH_ID, FULL_NAME, USERNAME, PASSWORD, EMAIL, CONTACT, USER_TYPE, STATUS, DATE_CREATED)
                                    VALUES 
                                    (@AccountId, @BranchId, @FullName, @Username, @Password, @Email, @Contact, @UserType, @Status, NOW())";

                using var cmd = new MySqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);
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
                    try
                    {
                        const string updateSql = @"UPDATE branches SET MANAGER = @ManagerName WHERE BRANCH_ID = @BranchId AND ACCOUNT_ID = @AccountId";
                        using var cmdUpdate = new MySqlCommand(updateSql, conn);
                        cmdUpdate.Parameters.AddWithValue("@ManagerName", fullName);
                        cmdUpdate.Parameters.AddWithValue("@BranchId", branchId);
                        cmdUpdate.Parameters.AddWithValue("@AccountId", accountId);
                        cmdUpdate.ExecuteNonQuery();
                    }
                    catch { }
                }

                return RedirectToAction("UserAccount");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
                ViewBag.Branches = GetBranchesDropdown(accountId);
                return View("CreateUser");
            }
        }

        // ===================== HELPER METHODS =====================
        private List<string> GetManagersList(string accountId)
        {
            var list = new List<string> { "-- Select Manager --" };
            if (string.IsNullOrEmpty(accountId)) return list;

            try
            {
                using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                const string sql = "SELECT FULL_NAME FROM user_accounts WHERE ACCOUNT_ID = @AccountId AND USER_TYPE = 'Manager' AND STATUS = 'Active'";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(reader["FULL_NAME"]?.ToString() ?? string.Empty);
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

                const string sql = "SELECT BRANCH_ID, BRANCH FROM branches WHERE ACCOUNT_ID = @AccountId ORDER BY BRANCH";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new
                    {
                        Id = reader["BRANCH_ID"]?.ToString() ?? string.Empty,
                        Name = reader["BRANCH"]?.ToString() ?? string.Empty
                    });
                }
            }
            catch { }

            return list;
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}