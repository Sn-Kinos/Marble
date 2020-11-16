using System.Collections.Generic;
using System.Text.Json;
using System.Windows.Forms;

namespace Marble
{
    class Land
    {
        private JsonElement data;

        private string owner = "";

        public Land(JsonElement _data)
        {
            data = _data;
        }

        public string getOwner()
        {
            return owner;
        }

        public string getType()
        {
            return data.GetProperty("type").ToString();
        }

        public int getBuyPrice()
        {
            return data.GetProperty("buy")[4].GetInt32();
        }

        public int getPayPrice()
        {
            return data.GetProperty("pay")[4].GetInt32();
        }
    }
}
