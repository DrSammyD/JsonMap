using System;
using System.Collections.Generic;

namespace JsonMap.Web.Models
{
    public interface ITest
    {
        long Number { get; set; }

        String Word { get; set; }

        List<ITest> SubModel { get; set; }
    }

    public class TestFirst : ITest
    {
        private long _number;
        private String _word;
        private List<ITest> _subModel;
        private long _extraNumber;

        public List<ITest> SubModel
        {
            get { return _subModel; }
            set { _subModel = value; }
        }

        public long ExtraNumber
        {
            get { return _extraNumber; }
            set { _extraNumber = value; }
        }

        public long Number
        {
            get { return _number; }
            set { _number = value; }
        }

        public String Word
        {
            get { return _word; }
            set { _word = value; }
        }
    }

    public class TestSecond : ITest
    {
        private long _number;
        private String _word;
        private List<ITest> _subModel;
        private String _extraWord;

        public List<ITest> SubModel
        {
            get { return _subModel; }
            set { _subModel = value; }
        }

        public String ExtraWord
        {
            get { return _extraWord; }
            set { _extraWord = value; }
        }

        public long Number
        {
            get { return _number; }
            set { _number = value; }
        }

        public String Word
        {
            get { return _word; }
            set { _word = value; }
        }
    }
}