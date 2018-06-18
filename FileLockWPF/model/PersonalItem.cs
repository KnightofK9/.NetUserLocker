using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileLockWPF
{
    public class PersonalItem
    {
        public List<String> imagePaths;
        public String name;
        public String personGroupId;
        public Guid guid;

        public static PersonalItem CreateNewPersonalItem(String name, List<String> imagePaths)
        {
            PersonalItem personalItem = new PersonalItem();
            personalItem.personGroupId = Constant.GROUP_ID;
            personalItem.name = name;
            personalItem.imagePaths = imagePaths;
            personalItem.guid = System.Guid.NewGuid();
            return personalItem;
        }
    }
}
