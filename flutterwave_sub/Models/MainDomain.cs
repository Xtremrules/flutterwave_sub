using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace flutterwave_sub.Models
{
    public class Service
    {
        public int Id { get; set; }
        public int PlanId { get; set; }
        [Required]
        public string Name { get; set; }
        public decimal Amount { get; set; }
        [Required]
        public string Interval { get; set; }
        [Required]
        public string Plan_token { get; set; }

        public int ManagerId { get; set; }
        public Manager Manager { get; set; }

        public virtual ICollection<Subs> Subs { get; set; }

        public virtual ICollection<Vendor> Vendors { get; set; }
    }

    public class Manager
    {
        public int Id { get; set; }
        [Required]
        public string AccountName { get; set; }

        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }
        public virtual ICollection<Service> Services { get; set; }
        public virtual ICollection<Vendor> Vendors { get; set; }
    }

    public class Subs
    {
        public int Id { get; set; }

        public string TxId { get; set; }

        public int ServiceId { get; set; }
        public virtual Service Service { get; set; }

        public int VendorId { get; set; }
        public virtual Vendor Vendor { get; set; }
    }

    public class Vendor
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }
        public int ManagerId { get; set; }
        public virtual Manager Manager { get; set; }
        public virtual ICollection<Service> Services { get; set; }
    }
}