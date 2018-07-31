namespace flutterwave_sub.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addedDateTime : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Managers", "dateTime", c => c.DateTime(nullable: false));
            AddColumn("dbo.Services", "dateTime", c => c.DateTime(nullable: false));
            AddColumn("dbo.Subs", "txRef", c => c.String());
            AddColumn("dbo.Subs", "flwRef", c => c.String());
            AddColumn("dbo.Subs", "raveRef", c => c.String());
            AddColumn("dbo.Subs", "dateTime", c => c.DateTime(nullable: false));
            AddColumn("dbo.Subs", "status", c => c.String());
            AddColumn("dbo.Vendors", "dateTime", c => c.DateTime(nullable: false));
            DropColumn("dbo.Subs", "TxId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Subs", "TxId", c => c.String());
            DropColumn("dbo.Vendors", "dateTime");
            DropColumn("dbo.Subs", "status");
            DropColumn("dbo.Subs", "dateTime");
            DropColumn("dbo.Subs", "raveRef");
            DropColumn("dbo.Subs", "flwRef");
            DropColumn("dbo.Subs", "txRef");
            DropColumn("dbo.Services", "dateTime");
            DropColumn("dbo.Managers", "dateTime");
        }
    }
}
