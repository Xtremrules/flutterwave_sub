using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace flutterwave_sub.Models
{
    public class Service
    {
        public int Id { get; set; }
        [Required]
        public int PlanId { get; set; }
        [Required]
        public string name { get; set; }
        public virtual ICollection<Sub> Subs { get; set; }
        public virtual ICollection<Manager> Managers { get; set; }
    }

    public class Manager
    {
        public int Id { get; set; }
        [Required]
        public string AccountName { get; set; }

        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        public virtual ICollection<Sub> Subs { get; set; }
        public virtual ICollection<Service> Services { get; set; }
    }

    public class Sub
    {
        public int Id { get; set; }
        public string Token { get; set; }

        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        public int ManagerId { get; set; }
        public virtual Manager Manager { get; set; }

        public virtual ICollection<Service> Services { get; set; }
    }
}