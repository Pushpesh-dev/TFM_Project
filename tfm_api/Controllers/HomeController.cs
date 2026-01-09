using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
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

        //Add an new user
        [HttpPost("add")]
        public async Task<IActionResult> AddUser([FromBody] Master AddUsers)
        {
            if (AddUsers == null)
            {
                return BadRequest("Invalid user data.");
            }
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
                await con.OpenAsync();
                int result = await Cmd.ExecuteNonQueryAsync();
                await con.CloseAsync();

                if (result > 0)
                {
                    try
                    {
                        string subject = "Welcom to the company";
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
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("getUsers")]
        public async Task<IActionResult> GetUsers()
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == "roleId");

            if (roleClaim == null)
                return Unauthorized("Required claims missing in token");

            if (!int.TryParse(roleClaim.Value, out int roleIdFromToken))
                return BadRequest("Invalid roleId in token");

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
                await connection.OpenAsync();
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    users.Add(new UserWithRole
                    {
                        Id = Convert.ToInt32(reader["Id"]),
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

                return Ok(users);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while fetching users");
            }
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
                            Id = Convert.ToInt32(reader["Id"]),
                            Email = reader["Email"].ToString(),
                            RoleId = Convert.ToInt32(reader["RoleId"]),
                            Name = reader["Name"].ToString()
                        };

                        var jwtToken = new JwtToken(_Config);
                        String token = jwtToken.GenerateJSONWebToken(user.Id, user.Email, user.RoleId);
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

        [HttpGet("get/{Id}")]
        public IActionResult GetEditData(int Id)
        {
            using (SqlCommand cmd = new SqlCommand("Edit", con))
            {
                con.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", Id);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var Master = new
                    {
                        Id = reader["Id"],
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

        [HttpPut("update/{Id}")]
        public IActionResult EditData(int Id, [FromBody] UpdateData newData)
        {
            using (SqlCommand cmd = new SqlCommand("Edit", con))
            {
                con.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", Id);

                SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    return NotFound(new { status = 404, message = "Product not found" });
                }
                reader.Close();

                using (SqlCommand Updatecmd = new SqlCommand("UpdateData", con))
                {

                    Updatecmd.CommandType = CommandType.StoredProcedure;
                    Updatecmd.Parameters.AddWithValue("@Id", Id);
                    Updatecmd.Parameters.AddWithValue("@Name", newData.Name);
                    Updatecmd.Parameters.AddWithValue("@Email", newData.Email);
                    Updatecmd.Parameters.AddWithValue("@Department", newData.Department);
                    Updatecmd.ExecuteNonQuery();
                }
            }
            con.Close();
            return Ok(new { status = 200, message = "User has been updated" });
        }

        [HttpDelete("delete/{Id}")]
        public async Task<IActionResult> DeleteUser(int Id)
        {
            using (SqlCommand cmd = new SqlCommand("UPDATE UsersData SET Deleted = 0 WHERE Id = @Id", con))
            {
                cmd.Parameters.AddWithValue("@Id", Id);

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

        [HttpGet("getUsers-test")]
        public IActionResult GetUsersTest()
        {
            return Ok("API is working");
        }

    }
}
