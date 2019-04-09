using HL7NhapiClient.Builders;
using NHapi.Base.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HL7NhapiClient
{
    public class AdtMessageFactory
    {
        public static IMessage CreateMessage(string messageType)
        {
            //This patterns enables you to build other message types 
            if (messageType.Equals("A01"))
            {
                return new AdtA01MessageBuilder().Build();
            }

            //if other types of ADT messages are needed, then implement your builders here
            throw new ArgumentException($"'{messageType}' is not supported yet. Extend this if you need to");
        }

       
    }
}
