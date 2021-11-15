using System.Configuration;

namespace HMS_Client
{
    public class HelicopterOperatorConfig : ConfigurationElement
    {
        public HelicopterOperatorConfig() { }

        public HelicopterOperatorConfig(HelicopterOperator helicopterOperator)
        {
            if (helicopterOperator != null)
            {
                id = helicopterOperator.id;
                name = helicopterOperator.name;
                email = helicopterOperator.email;
            }
        }

        [ConfigurationProperty("id", DefaultValue = 0, IsRequired = true, IsKey = true)]
        [IntegerValidator(MinValue = 0, MaxValue = int.MaxValue)]
        public int id
        {
            get { return (int)this["id"]; }
            set { this["id"] = value; }
        }

        [ConfigurationProperty("name", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }


        [ConfigurationProperty("email", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string email
        {
            get { return (string)this["email"]; }
            set { this["email"] = value; }
        }
    }
}
