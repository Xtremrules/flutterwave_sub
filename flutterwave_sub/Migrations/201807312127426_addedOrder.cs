namespace flutterwave_sub.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addedOrder : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Subs", "orderRef", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Subs", "orderRef");
        }
    }
}
