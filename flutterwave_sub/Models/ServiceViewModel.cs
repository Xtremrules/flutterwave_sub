using System.ComponentModel.DataAnnotations;

namespace flutterwave_sub.Models
{
    public class ServiceViewModel
    {
        public int Id { get; set; }
        public int PlanId { get; set; }
        public decimal amount { get; set; }
        public string interval { get; set; }
        public string name { get; set; }
        public bool active { get; set; }
    }

    public class ServiceAddModel
    {
        [Required]
        public string name { get; set; }
        [Required]
        public decimal amount { get; set; }
        [Required]
        public int ManagerId { get; set; }
        [Required]
        public string interval { get; set; }
        public string seckey { get; set; }
    }
}