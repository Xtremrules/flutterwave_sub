using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace flutterwave_sub.Models
{
    public class Service
    {
        public int id { get; set; }
        [Required]
        public int plan_id { get; set; }
        [Required]
        public string name { get; set; }
        public virtual ICollection<Sub> Subs { get; set; }
        public virtual ICollection<Manager> Managers { get; set; }
    }

    public class Manager
    {
        public int id { get; set; }
        [Required]
        public string account_name { get; set; }

        public string ApplicationUserid { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        public virtual ICollection<Sub> Subs { get; set; }
        public virtual ICollection<Service> Services { get; set; }
    }

    public class Sub
    {
        public int id { get; set; }
        public string token { get; set; }

        public string ApplicationUserid { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        public int Managerid { get; set; }
        public virtual Manager Manager { get; set; }

        public virtual ICollection<Service> Services { get; set; }
    }
}