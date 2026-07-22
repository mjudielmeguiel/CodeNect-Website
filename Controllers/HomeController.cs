using CodeNect_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

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
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                string connectionString = _config.GetConnectionString("DefaultConnection")!;
                using var conn = new MySqlConnection(connectionString);
                try
                {
                    conn.Open();
                    string sql = "SELECT * FROM ACCOUNT WHERE USERNAME = @USERNAME AND PASSWORD = @PASSWORD AND STATUS = 'ACTIVE'";
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@USERNAME", model.Username);
                    cmd.Parameters.AddWithValue("@PASSWORD", model.Password);

                    if (cmd.ExecuteReader().Read())
                        return RedirectToAction("Dashboard");
                    else
                        ViewBag.Error = "❌ Invalid Username or Password! Please try again.";
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "⚠️ Database Error: " + ex.Message;
                }
            }
            return View("Index");
        }

        public IActionResult Dashboard()
        {
            List<BranchModel> branches = new List<BranchModel>();
            string connectionString = _config.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(connectionString);
            try
            {
                conn.Open();
                string sql = "SELECT BRANCH_ID, BRANCH, BUSINESS_TYPE, CONTACT, EMAIL, MANAGER, REGISTRATION_DATE, STATUS FROM branches ORDER BY BRANCH";

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    branches.Add(new BranchModel
                    {
                        BRANCH_ID = reader.GetInt32("BRANCH_ID"),
                        BRANCH = reader["BRANCH"]?.ToString() ?? "",
                        BUSINESS_TYPE = reader["BUSINESS_TYPE"]?.ToString() ?? "",
                        CONTACT = reader["CONTACT"]?.ToString() ?? "",
                        EMAIL = reader["EMAIL"]?.ToString() ?? "",
                        MANAGER = reader["MANAGER"]?.ToString() ?? "",
                        REGISTRATION_DATE = reader.GetDateTime("REGISTRATION_DATE"),
                        STATUS = reader["STATUS"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "⚠️ Error loading branches: " + ex.Message;
            }

            ViewBag.Branches = branches;
            return View();
        }

        // ===== CREATE BRANCH =====
        public IActionResult CreateBranch()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateBranch(BranchModel model)
        {
            if (ModelState.IsValid)
            {
                string connectionString = _config.GetConnectionString("DefaultConnection")!;
                using var conn = new MySqlConnection(connectionString);
                try
                {
                    conn.Open();

                    // Check if branch name already exists
                    var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM branches WHERE BRANCH = @BRANCH", conn);
                    checkCmd.Parameters.AddWithValue("@BRANCH", model.BRANCH);
                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                    {
                        ViewBag.Error = "⚠️ Branch name already exists! Please use another name.";
                        return View();
                    }

                    // Insert new branch - matches your table columns exactly
                    string sql = @"
                        INSERT INTO branches 
                        (BRANCH, BUSINESS_TYPE, CONTACT, EMAIL, MANAGER, TIN, REGISTRATION_DATE, STATUS)
                        VALUES 
                        (@BRANCH, @BUSINESS_TYPE, @CONTACT, @EMAIL, @MANAGER, @TIN, CURDATE(), @STATUS)";

                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@BRANCH", model.BRANCH);
                    cmd.Parameters.AddWithValue("@BUSINESS_TYPE", model.BUSINESS_TYPE);
                    cmd.Parameters.AddWithValue("@CONTACT", model.CONTACT);
                    cmd.Parameters.AddWithValue("@EMAIL", string.IsNullOrEmpty(model.EMAIL) ? DBNull.Value : model.EMAIL);
                    cmd.Parameters.AddWithValue("@MANAGER", string.IsNullOrEmpty(model.MANAGER) ? DBNull.Value : model.MANAGER);
                    cmd.Parameters.AddWithValue("@TIN", string.IsNullOrEmpty(model.TIN) ? DBNull.Value : model.TIN);
                    cmd.Parameters.AddWithValue("@STATUS", model.STATUS);

                    cmd.ExecuteNonQuery();
                    ViewBag.Success = "✅ Branch created successfully!";
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "❌ Error: " + ex.Message;
                }
            }
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(AccountModel model)
        {
            if (ModelState.IsValid)
            {
                string connectionString = _config.GetConnectionString("DefaultConnection")!;
                using var conn = new MySqlConnection(connectionString);
                try
                {
                    conn.Open();
                    var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM ACCOUNT WHERE USERNAME = @USERNAME", conn);
                    checkCmd.Parameters.AddWithValue("@USERNAME", model.USERNAME);

                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                    {
                        ViewBag.Error = "⚠️ Username already exists! Please choose another one.";
                        return View();
                    }

                    string sql = @"
                        INSERT INTO ACCOUNT 
                        (SERIAL_NUMBER, ACCOUNT_ID, ACCOUNT, ADDRESS, CONTACT, EMAIL, USERNAME, PASSWORD, STATUS, CREATE_AT)
                        VALUES 
                        (@SERIAL_NUMBER, @ACCOUNT_ID, @ACCOUNT, @ADDRESS, @CONTACT, @EMAIL, @USERNAME, @PASSWORD, 'ACTIVE', CURDATE())";

                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@SERIAL_NUMBER", new Random().Next(100000, 999999));
                    cmd.Parameters.AddWithValue("@ACCOUNT_ID", new Random().Next(10000, 99999));
                    cmd.Parameters.AddWithValue("@ACCOUNT", model.ACCOUNT);
                    cmd.Parameters.AddWithValue("@ADDRESS", model.ADDRESS);
                    cmd.Parameters.AddWithValue("@CONTACT", model.CONTACT);
                    cmd.Parameters.AddWithValue("@EMAIL", model.EMAIL);
                    cmd.Parameters.AddWithValue("@USERNAME", model.USERNAME);
                    cmd.Parameters.AddWithValue("@PASSWORD", model.PASSWORD);

                    cmd.ExecuteNonQuery();
                    ViewBag.Success = "✅ Account created successfully! You can now login.";
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "❌ Error: " + ex.Message;
                }
            }
            return View();
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Index");
        }
    }
}