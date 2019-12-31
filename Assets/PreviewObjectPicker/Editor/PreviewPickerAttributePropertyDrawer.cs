using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fyrvall.PreviewObjectPicker
{
    [CustomPropertyDrawer(typeof(PreviewPickerPropertyAttribute))]
    public class PreviewPickerPropertyAttributePropertyDrawer : PreviewPickerBasePropertyDrawer
    {
    }
}