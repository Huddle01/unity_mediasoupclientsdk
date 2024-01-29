using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IJsonObject<T>
{
    string ConvertToJSON();

    static T FromJson(string jsonString)=> throw new NotImplementedException();

}
