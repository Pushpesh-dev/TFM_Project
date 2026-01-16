using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;

//using tfm_web.Filters;
using tfm_web.Models;
using tfm_web.Services;

namespace tfm_web.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        //private readonly string ConnectionStrings;
        private readonly IConfiguration _Config;
        private readonly EmailService _emailService;
        private SqlConnection con;
        public HomeController(IConfiguration configuration, EmailService emailService)
        {
            _Config = configuration;
            _emailService = emailService;
            con = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        }

        private string GetWelcomeEmailSubject()
        {
            return "Welcome to the company";
        }

        //Add an new user
        [Authorize]
        [HttpPost("add")]
        public async Task<IActionResult> AddUser([FromBody] Master AddUsers)
        {
            if (AddUsers == null)
            {
                return BadRequest("Invalid user data.");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User not logged in.");

            int createdBy = int.Parse(userIdClaim);

            var hasher = new PasswordHasher<object>();
            string passwordHash = hasher.HashPassword(null, AddUsers.Password);

            using (SqlCommand Cmd = new SqlCommand("AddUser", con))
            {

                Cmd.CommandType = CommandType.StoredProcedure;
                Cmd.Parameters.AddWithValue("@Name", AddUsers.Name);
                Cmd.Parameters.AddWithValue("@Email", AddUsers.Email);
                Cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                Cmd.Parameters.AddWithValue("@Department", AddUsers.Department);
                Cmd.Parameters.AddWithValue("@JoiningDate", AddUsers.JoiningDate);
                Cmd.Parameters.AddWithValue("@RoleId", AddUsers.RoleId);
                Cmd.Parameters.Add("@CreatedBy", SqlDbType.Int).Value = createdBy;

                await con.OpenAsync();
                int result = await Cmd.ExecuteNonQueryAsync();
                await con.CloseAsync();

                if (result > 0)
                {
                    try
                    {
                        string subject = GetWelcomeEmailSubject();
                        string Body = $@"
                            <h2> Hi {AddUsers.Name},</h2>
                            <p>Welcome to the team! Your account has been created. </p>
                            <p>Your login email: {AddUsers.Email}</p>
                            <p>Best regards,<br/>Company HR</p>     
                           ";
                        await _emailService.SendEmailAsync(AddUsers.Email, subject, Body);
                    }
                    catch (Exception ex)
                    {
                        // Log error but still return success for user creation
                        Console.WriteLine($"Email sending failed: {ex.Message}");
                    }
                    return Ok(new { status = 200, message = "User Add Sucessfully!" });
                }
                else
                {

                    return StatusCode(500, "Error adding user.");
                }
            }
        }

        //add role
        [HttpPost("addRole")]
        public async Task<IActionResult> AddRole(role RoleModel)
        {
            using (SqlCommand Cmd = new SqlCommand("INSERT INTO Master.Role (RoleName) VALUES (@RoleName)", con))
            {
                Cmd.Parameters.AddWithValue("@RoleName", RoleModel.RoleName);

                await con.OpenAsync();
                int result = await Cmd.ExecuteNonQueryAsync();
                await con.CloseAsync();

                if (result > 0)
                    return Ok("Role Added successfully");

                else
                    return StatusCode(500, "Error adding role.");
            }
        }

        //get  data
        [Authorize(Roles = "Admin,User")]
        [HttpGet("getUsers")]
        public async Task<ApiResponse<List<UserWithRole>>> GetUsers()
        {
            var response = new ApiResponse<List<UserWithRole>>();
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == "roleId");
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (roleClaim == null || userIdClaim == null)
            {
                response.Success = false;
                response.Message = "Required claims missing in token";
                return response;
            }

            if (!int.TryParse(roleClaim.Value, out int roleIdFromToken) ||
             !int.TryParse(userIdClaim.Value, out int userIdFromToken))
            {
                response.Success = false;
                response.Message = "Invalid roleId in token";
                return response;
            }


            var users = new List<UserWithRole>();

            try
            {
                await using var connection = new SqlConnection(con.ConnectionString);
                await using var command = new SqlCommand("GetUserDetailsById", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // ✅ Pass JWT-based parameters (secure)
                command.Parameters.Add("@RoleId", SqlDbType.Int).Value = roleIdFromToken;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userIdFromToken;
                await connection.OpenAsync();
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    users.Add(new UserWithRole
                    {
                        UserId = Convert.ToInt32(reader["UserId"]),
                        Name = reader["Name"]?.ToString() ?? string.Empty,
                        Email = reader["Email"]?.ToString() ?? string.Empty,
                        Department = reader["Department"]?.ToString() ?? string.Empty,
                        JoiningDate = reader["JoiningDate"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["JoiningDate"]),
                        RoleId = Convert.ToInt32(reader["RoleId"]),
                        RoleName = reader["RoleName"]?.ToString() ?? string.Empty,
                        Deleted = Convert.ToInt32(reader["Deleted"]) == 1
                    });
                } 
                response.Success = true;
                response.Message = "Users fetched successfully";
                response.Data = users;
            }

            catch
            {
                response.Success = false;
                response.Message = "An error occurred while fetching";
            }
            return response;

        }

        //login
        [HttpPost("signIn")]
        public async Task<IActionResult> SignIn([FromBody] Login login)
        {
            using (SqlCommand cmd = new SqlCommand("LoginData", con))
            { 

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", login.Email);
                await con.OpenAsync();

                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    string storedHash = reader["PasswordHash"].ToString();
                    var hasher = new PasswordHasher<object>();
                    var result = hasher.VerifyHashedPassword(null, storedHash, login.Password);
                    if (result == PasswordVerificationResult.Success)
                    {
                        var user = new
                        {

                            UserId = Convert.ToInt32(reader["UserId"]),
                            Email = reader["Email"].ToString(),
                            RoleId = Convert.ToInt32(reader["RoleId"]),
                            RoleName = reader["RoleName"].ToString(),
                            Name = reader["Name"].ToString()
                        };

                        var jwtToken = new JwtToken(_Config);
                        String token = jwtToken.GenerateJSONWebToken(user.UserId, user.Email, user.RoleName, user.RoleId);
                        return Ok(new { user, token });
                    }

                    else
                    {
                        return Unauthorized("Invalid credentials");
                    }
                }
                else
                {
                    return Unauthorized("Invalid credentials");
                }
            }
        }

        [HttpGet("get/{UserId}")]
        public IActionResult GetEditData(int UserId)
        {
            using (SqlCommand cmd = new SqlCommand("Edit", con))
            {
                con.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", UserId);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var Master = new
                    {
                        UserId = reader["UserId"],
                        Name = reader["Name"],
                        Email = reader["Email"],
                        Department = reader["Department"]
                    };
                    con.Close();
                    return Ok(new[] { Master });
                }
                con.Close();
                return NotFound("User Not Found");
            }
        }

        [HttpPut("update/{UserId}")]
        public IActionResult EditData(int UserId, [FromBody] UpdateData newData)
        {
            using (SqlCommand cmd = new SqlCommand("Edit", con))
            {
                con.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", UserId);

                SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    return NotFound(new { status = 404, message = "Product not found" });
                }
                reader.Close();

                using (SqlCommand Updatecmd = new SqlCommand("UpdateData", con))
                {

                    Updatecmd.CommandType = CommandType.StoredProcedure;
                    Updatecmd.Parameters.AddWithValue("@UserId", UserId);
                    Updatecmd.Parameters.AddWithValue("@Name", newData.Name);
                    Updatecmd.Parameters.AddWithValue("@Email", newData.Email);
                    Updatecmd.Parameters.AddWithValue("@Department", newData.Department);
                    Updatecmd.ExecuteNonQuery();
                }
            }
            con.Close();
            return Ok(new { status = 200, message = "User has been updated" });
        }

        [HttpDelete("delete/{UserId}")]
        public async Task<IActionResult> DeleteUser(int UserId)
        {
            using (SqlCommand cmd = new SqlCommand("UPDATE UsersData SET Deleted = 0 WHERE UserId = @UserId", con))
            {
                cmd.Parameters.AddWithValue("@UserId", UserId);

                await con.OpenAsync();
                int rowAffected = await cmd.ExecuteNonQueryAsync();
                await con.CloseAsync();

                if (rowAffected == 0)
                {
                    return NotFound("User not found");
                }
                return Ok(new { Status = 200, message = "User Deleted!" });
            }
        }

        //AddProduct
        [HttpPost("addProduct")]
        public async Task<IActionResult> AddProdAddProduct([FromBody] Products AddProd)
        {
            int result = 0;
            using(SqlCommand cmd = new SqlCommand("AddProducts", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProductName", AddProd.ProductName);
                cmd.Parameters.AddWithValue("@Price", AddProd.Price);
                cmd.Parameters.AddWithValue("@StockQuantity", AddProd.StockQuantity);
                cmd.Parameters.AddWithValue("@Description", AddProd.Description);
                cmd.Parameters.AddWithValue("@IsActive", AddProd.IsActive);
                cmd.Parameters.AddWithValue("@CreatedDate", AddProd.CreatedDate);
                cmd.Parameters.AddWithValue("@ExpireDate", AddProd.ExpireDate);

                await con.OpenAsync();
                result =  await cmd.ExecuteNonQueryAsync();
                await con.CloseAsync();
            }
                return Ok(new { success = result > 0, message = result > 0 ? "Add Successfully!" : "Failed to add product" });
            
        }


        [HttpGet("getProduct")]
        public async Task<IActionResult> GetProducts()
        {
            List<Products> products = new List<Products>();
            using(SqlCommand cmd = new SqlCommand("GetProduct",con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                await con.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    products.Add(new Products
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        ProductName = reader["ProductName"].ToString(),
                        Price = Convert.ToDecimal(reader["Price"]),
                        StockQuantity = Convert.ToInt32(reader["StockQuantity"]),
                        Description = reader["Description"].ToString(),
                        IsActive = Convert.ToInt32(reader["IsActive"]),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        ExpireDate = Convert.ToDateTime(reader["ExpireDate"])
                    });
                }
                await con.CloseAsync();
                return Ok(new { Status = 200, Message = "Success",products});

            }
        }

    }
}
