using HL7NhapiClient.Builders;
using NHapi.Base.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HL7NhapiClient
{
    public class AdtMessageFactory
    {
        static string[] files = new string[] { Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Test Files", "Sample Pathology Lab Report.pdf"),        
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Test Files",
                "Java Senior.pdf"),
          Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Test Files",
                "Factura Servicios Profesionales  SONDA 2019- 15032019.pdf"),
          Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Test Files",
                "FHIR-Fundamentals_2019.pdf"),
          Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Test Files",
                "Scrum Master URIEL OLAYA.pdf")};

        static int i = -1;
        public static IMessage CreateMessage(string messageType)
        {
            //This patterns enables you to build other message types 
            if (messageType.Equals("A01"))
            {
                return new AdtA01MessageBuilder().Build();
            }
            else if (messageType.Equals("R01"))
            {
                i++;
                return new OruR01MessageBuilder().Build(files[i]);
            }

            //if other types of ADT messages are needed, then implement your builders here
            throw new ArgumentException($"'{messageType}' is not supported yet. Extend this if you need to");
        }

       
    }
}
