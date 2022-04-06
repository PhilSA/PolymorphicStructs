using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PolymorphicStructs
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class PolymorphicStruct : System.Attribute
    {
        public string ImplementedInterfaces; 

        public PolymorphicStruct(string implementedInterfaces = "")
        {
            ImplementedInterfaces = implementedInterfaces;
        }
    }
}