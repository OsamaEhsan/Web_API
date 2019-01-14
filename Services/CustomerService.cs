using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web_API.Entities;
using Web_API.Helpers;

namespace Web_API.Services
{
    public interface ICustomerService {
        Customer Authenticate(string username, string password);
        IEnumerable<Customer> GetAll();
        Customer Create(Customer customer, string password);
        Customer GetById(int id);
    }
    public class CustomerService : ICustomerService {

        private DataContext _context;
        public CustomerService(DataContext context) {
            _context = context;
        }
        public Customer Authenticate(string username, string password) {

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            var user = _context.customers.SingleOrDefault(x => x.username == username);

            if (user == null)
                return null;

            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;

            return user;
        }
        public IEnumerable<Customer> GetAll() {
            return _context.customers;
        }
        public Customer GetById(int id)
        {
            return _context.customers.Find(id);
        }
        public Customer Create(Customer customer, string password) {
            if (string.IsNullOrWhiteSpace(password))
                throw new AppException("Password is required");

            if (_context.customers.Any(x => x.username == customer.username))
                throw new AppException("Username \"" + customer.username + "\" is already taken");

            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            customer.PasswordHash = passwordHash;
            customer.PasswordSalt = passwordSalt;

            _context.customers.Add(customer);
            _context.SaveChanges();

            return customer;
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }

            return true;
        }
    }
}
