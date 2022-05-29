/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;

namespace CommonUtilities.Geometry
{
    public struct SupportedFileFormat
    {
        public string Format;
        public string Description;
        public string MasterFormat; //Mostly assembly file
        public string[] RelativeFormats;
        public bool bOnlyReferenceable; //Mostly for part files

        public SupportedFileFormat(string _Format, string _Description, string _MasterFormat, string[] _RelativeFormats, bool _bOnlyReferenceable)
        {
            Format = _Format;
            Description = _Description;
            MasterFormat = _MasterFormat;
            RelativeFormats = _RelativeFormats;
            bOnlyReferenceable = _bOnlyReferenceable;
        }
    }

    public static class SupportedFileFormats
    {
        public static readonly List<SupportedFileFormat> List = new List<SupportedFileFormat>()
        {
            new SupportedFileFormat("nwd", "Navisworks File",                                       null,               new string[] { "nwd" },                                     false),

            new SupportedFileFormat("zip", "Archive of Multiple CAD Files",                         null,               new string[] { "zip" },                                     false),

            new SupportedFileFormat("3ds", "3DS Max File",                                          null,               new string[] { "3ds" },                                     false),

            new SupportedFileFormat("dri", "PDS Piping Designer File",                              null,               new string[] { "dri" },                                     false),

            //TODO: Test bOnlyReferenceable for CATIA files
            new SupportedFileFormat("model", "CATIA 3D Model File",                                 "catproduct",       new string[] { "catproduct", "catpart", "cgr", "model" },   false),
            new SupportedFileFormat("cgr", "CATIA Graphical Representation File",                   "catproduct",       new string[] { "catproduct", "catpart", "cgr", "model" },   false),
            new SupportedFileFormat("catpart", "CATIA V5 Part File",                                "catproduct",       new string[] { "catproduct", "catpart", "cgr", "model" },   true),
            new SupportedFileFormat("catproduct", "CATIA V5 Assembly File",                         "catproduct",       new string[] { "catproduct", "catpart", "cgr", "model" },   false),

            new SupportedFileFormat("stp", "CIS/2 File",                                            null,               new string[] { "stp" },                                     false),

            new SupportedFileFormat("dgn", "MicroStation Design File",                              null,               new string[] { "dgn" },                                     false),

            //The Autodesk DWG/DXF file loader plugin 
            new SupportedFileFormat("dwg", "AutoCAD Drawing Database File",                         null,               new string[] { "dwg", "dxf" },                              false),
            new SupportedFileFormat("dxf", "AutoCAD Drawing Exchange Format File",                  null,               new string[] { "dwg", "dxf" },                              false),

            new SupportedFileFormat("fls", "FARO File",                                             null,               new string[] { "fls" },                                     false),

            new SupportedFileFormat("fbx", "FBX File",                                              null,               new string[] { "fbx" },                                     false),

            new SupportedFileFormat("ifc", "Industry Foundation Classes File",                      null,               new string[] { "ifc" },                                     false),

            new SupportedFileFormat("igs", "IGES Drawing File (1)",                                 null,               new string[] { "igs", "iges" },                             false),
            new SupportedFileFormat("iges", "IGES Drawing File (2)",                                null,               new string[] { "igs", "iges" },                             false),

            //TODO: Test bOnlyReferenceable for Inventor files
            new SupportedFileFormat("ipt", "Inventor 3D Part File", "iam",                                              new string[] { "ipt", "iam" },                              true),
            new SupportedFileFormat("iam", "Inventor 3D Assembly Model Data File",                  "iam",              new string[] { "ipt", "iam" },                              false),

            new SupportedFileFormat("jt", "Siemens PLM Software File",                              null,               new string[] { "jt" },                                      false),

            //TODO: Test bOnlyReferenceable for Pro/ENGINEER files
            new SupportedFileFormat("prt", "Pro/ENGINEER NX File",                                  "asm",              new string[] { "prt", "asm" },                              true),
            new SupportedFileFormat("asm", "Pro/ENGINEER ASM File",                                 "asm",              new string[] { "prt", "asm" },                              false),

            new SupportedFileFormat("x_b", "Parasolid Binary File",                                 null,               new string[] { "x_b" },                                     false),

            new SupportedFileFormat("rcs", "ReCap File (1)",                                        null,               new string[] { "rcs", "rcp" },                              false),
            new SupportedFileFormat("rcp", "ReCap File (2)",                                        null,               new string[] { "rcs", "rcp" },                              false),

            new SupportedFileFormat("rvt", "Revit File",                                            null,               new string[] { "rvt" },                                     false),

            new SupportedFileFormat("3dm", "Rhino File",                                            null,               new string[] { "3dm" },                                     false),

            new SupportedFileFormat("att", "AVEVA PDMS Attribute File",                             null,               new string[] { "att", "attrib", "txt", "rvm" },             true),
            new SupportedFileFormat("attrib", "AVEVA PDMS Attribute File",                          null,               new string[] { "att", "attrib", "txt", "rvm" },             true),
            new SupportedFileFormat("txt", "AVEVA PDMS Attribute File",                             null,               new string[] { "att", "attrib", "txt", "rvm" },             true),
            new SupportedFileFormat("rvm", "AVEVA PDMS File",                                       null,               new string[] { "att", "attrib", "txt", "rvm" },             false),

            new SupportedFileFormat("sat", "ACIS SAT File",                                         null,               new string[] { "sat" },                                     false),

            new SupportedFileFormat("skp", "SketchUp File",                                         null,               new string[] { "skp" },                                     false),

            new SupportedFileFormat("sldprt", "SolidWorks Part File",                               "sldasm",           new string[] { "sldprt", "sldasm" },                        true),
            new SupportedFileFormat("sldasm", "SolidWorks Assembly File",                           "sldasm",           new string[] { "sldprt", "sldasm" },                        false),

            new SupportedFileFormat("step", "STEP Standard for the Exchange of Product Data File",  null,               new string[] { "step" },                                    false),

            new SupportedFileFormat("stl", "3D Systems STL File",                                   null,               new string[] { "stl" },                                     false),

            new SupportedFileFormat("vue", "Smart Plant 3D File",                                   null,               new string[] { "vue" },                                     false),

            new SupportedFileFormat("wrl", "VRML Virtual Reality Modeling Language Plain",          null,               new string[] { "wrl", "wrz" },                              false),
            new SupportedFileFormat("wrz", "VRML Virtual Reality Modeling Language Compressed",     null,               new string[] { "wrl", "wrz" },                              false)
        };

        private static bool bDictionarySet = false;
        private static readonly Dictionary<string, Tuple<string, string, string[], bool>> FormatDictionary = new Dictionary<string, Tuple<string, string, string[], bool>>();
        private static void DictionarySetup()
        {
            if (!bDictionarySet)
            {
                foreach (var FileType in List)
                {
                    FormatDictionary[FileType.Format] = new Tuple<string, string, string[], bool>(FileType.Description, FileType.MasterFormat, FileType.RelativeFormats, FileType.bOnlyReferenceable);
                }
                bDictionarySet = true;
            }
        }

        public static string GetDescription(string _Format)
        {
            DictionarySetup();
            _Format = _Format.ToLower().TrimStart('.');

            FormatDictionary.TryGetValue(_Format, out Tuple<string, string, string[], bool> Result);
            return Result.Item1;
        }

        public static string GetMasterFormat(string _Format)
        {
            DictionarySetup();
            _Format = _Format.ToLower().TrimStart('.');

            if (FormatDictionary.TryGetValue(_Format, out Tuple<string, string, string[], bool> Result))
            {
                return Result.Item2 ?? _Format;
            }
            return null;
        }

        public static bool IsReferenceableOnly(out bool _bReferenceableOnly, string _Format)
        {
            DictionarySetup();
            _Format = _Format.ToLower().TrimStart('.');

            if (FormatDictionary.TryGetValue(_Format, out Tuple<string, string, string[], bool> Result))
            {
                _bReferenceableOnly = Result.Item4;
                return true;
            }

            _bReferenceableOnly = false;
            return false;
        }

        public static string[] GetRelativeFormats(string _Format)
        {
            DictionarySetup();
            _Format = _Format.ToLower().TrimStart('.');

            if (FormatDictionary.TryGetValue(_Format, out Tuple<string, string, string[], bool> Result))
            {
                return Result.Item3;
            }
            return null;
        }

        public static bool IsSupported(string _Format)
        {
            DictionarySetup();
            _Format = _Format.ToLower().TrimStart('.');

            return FormatDictionary.TryGetValue(_Format, out Tuple<string, string, string[], bool> Result);
        }

        private static string GetFileFormatFromFilePath(string _FilePath)
        {
            int Ix = _FilePath.LastIndexOf('.');
            Ix = Ix < 0 ? 0 : (Ix + 1);
            return _FilePath.Substring(Ix).ToLower();
        }

        public static bool IsFormatRelatedToFirstInList(out bool _bRelatedOrArrayEmpty, string _Format, List<string> _ListParameter)
        {
            DictionarySetup();
            _Format = _Format.ToLower().TrimStart('.');

            _bRelatedOrArrayEmpty = true;

            if (_ListParameter != null && _ListParameter.Count > 0)
            {
                string FirstElementFormat = GetFileFormatFromFilePath(_ListParameter[0]);

                if (FormatDictionary.TryGetValue(FirstElementFormat, out Tuple<string, string, string[], bool> Result))
                {
                    foreach (string Current in Result.Item3)
                    {
                        if (Current == _Format)
                        {
                            return true;
                        }
                    }
                    _bRelatedOrArrayEmpty = false;
                    return true;
                }
                _bRelatedOrArrayEmpty = false;
                return false;
            }
            return true;
        }

        public static bool SortByMastersFirst(out List<string> _ResultList, List<string> _ListParameter)
        {
            DictionarySetup();

            if (_ListParameter != null && _ListParameter.Count > 0)
            {
                string FirstElement = GetFileFormatFromFilePath(_ListParameter[0]);

                string MasterFormat = GetMasterFormat(FirstElement);
                if (MasterFormat == null)
                {
                    _ResultList = new List<string>(_ListParameter);
                    return false;
                }

                _ResultList = new List<string>();
                foreach (var Current in _ListParameter)
                {
                    string CurrentFormat = GetFileFormatFromFilePath(Current);

                    if (CurrentFormat == MasterFormat)
                    {
                        _ResultList.Insert(0, Current);
                    }
                    else
                    {
                        _ResultList.Add(Current);
                    }
                }
            }
            else
            {
                _ResultList = new List<string>();
            }
            return true;
        }

        //Excludes files which has to be in the same directory but should not be added as an input to the converter. Like parts etc.
        public static List<string> ExcludeReferenceableOnlyFiles(List<string> _ListParameter)
        {
            DictionarySetup();

            //First iteration is for checking if there is any non-referenceable only file
            bool bContainsNonReferenceableOnlyFile = false;
            foreach (string Current in _ListParameter)
            {
                string CurrentFormat = GetFileFormatFromFilePath(Current);

                if (IsReferenceableOnly(out bool bReferenceableOnly, CurrentFormat) && !bReferenceableOnly)
                {
                    bContainsNonReferenceableOnlyFile = true;
                    break;
                }
            }

            if (!bContainsNonReferenceableOnlyFile)
            {
                return new List<string>(_ListParameter);
            }

            //Second iteration is for excluding
            var NewList = new List<string>();
            foreach (string Current in _ListParameter)
            {
                string CurrentFormat = GetFileFormatFromFilePath(Current);

                if (IsReferenceableOnly(out bool bReferenceableOnly, CurrentFormat) && !bReferenceableOnly)
                {
                    NewList.Add(Current);
                }
            }
            return NewList;
        }
    }
}