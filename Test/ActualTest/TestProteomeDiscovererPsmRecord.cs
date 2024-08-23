using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.External;
using Readers;

namespace Test
{
    public class TestProteomeDiscovererPsmRecord
    {
        public static string PsmFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData",
            "ProteomeDiscoverer_TestData_Psms.txt");
        public static string PeptideFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData",
                           "ProteomeDiscoverer_TestData_PeptideGroups.txt");

        [Test]
        public static void TestFullSequenceConversion_Psm()
        {
            var psms = new ProteomeDiscovererPsmFile(PsmFilePath);
            psms.LoadResults();

            for (int i = 0; i < psms.Count(); i++)
            {
                var psm = psms.ElementAt(i);
                Assert.That(psm.FullSequence, Is.Not.Null);
                switch (i)
                {
                    case 0:
                        Assert.That(psm.AnnotatedSequence, Is.EqualTo("ESLQDTQPVGVLVDccK"));
                        Assert.That(psm.BaseSequence, Is.EqualTo("ESLQDTQPVGVLVDCCK"));
                        Assert.That(psm.FullSequence, Is.EqualTo("ESLQDTQPVGVLVDC[Common Fixed:Carbamidomethyl on C]C[Common Fixed:Carbamidomethyl on C]K"));
                        break;
                    case 1:
                        Assert.That(psm.AnnotatedSequence, Is.EqualTo("LESGMQNmSIHTK"));
                        Assert.That(psm.BaseSequence, Is.EqualTo("LESGMQNMSIHTK"));
                        Assert.That(psm.FullSequence, Is.EqualTo("LESGMQNM[Common Variable:Oxidation on M]SIHTK"));
                        break;
                    case 2:
                        Assert.That(psm.AnnotatedSequence, Is.EqualTo("RFVEVGR"));
                        Assert.That(psm.BaseSequence, Is.EqualTo("RFVEVGR"));
                        Assert.That(psm.FullSequence, Is.EqualTo("RFVEVGR"));
                        break;
                    case 3:
                        Assert.That(psm.AnnotatedSequence, Is.EqualTo("SsSPAPADIAQTVQEDLR"));
                        Assert.That(psm.BaseSequence, Is.EqualTo("SSSPAPADIAQTVQEDLR"));
                        Assert.That(psm.FullSequence, Is.EqualTo("SS[Common Biological:Phosphorylation on S]SPAPADIAQTVQEDLR"));
                        break;
                }
            }
        }

        [Test]
        public static void TestFullSequenceConversion_Peptide()
        {
            var psms = new ProteomeDiscovererPsmFile(PsmFilePath);
            psms.LoadResults();

            for (int i = 0; i < psms.Count(); i++)
            {
                var psm = psms.ElementAt(i);
                Assert.That(psm.FullSequence, Is.Not.Null);
                switch (i)
                {
                    case 0:
                        Assert.That(psm.FullSequence, Is.EqualTo("DHDSYGVDKK"));
                        break;
                    case 1:
                        Assert.That(psm.FullSequence, Is.EqualTo("LAHEDAEC[Common Fixed:Carbamidomethyl on C]EK"));
                        break;
                    case 2:
                        Assert.That(psm.FullSequence, Is.EqualTo("HSS[ on S]PHQSEDEEDPR"));
                        break;
                }
            }
        }

        [Test]
        public static void TestModificationsConversion_Psms()
        {
            var psms = new ProteomeDiscovererPsmFile(PsmFilePath);
            psms.LoadResults();

            for (int i = 0; i < psms.Count(); i++)
            {
                var psm = psms.ElementAt(i);
                switch (i)
                {
                    case 0:
                        Assert.That(psm.Modifications.Length, Is.EqualTo(2));
                        Assert.That(psm.Modifications[0].ModLocation, Is.EqualTo(15));
                        Assert.That(psm.Modifications[0].ModifiedResidue, Is.EqualTo('C'));
                        Assert.That(psm.Modifications[0].ModName, Is.EqualTo("Carbamidomethyl"));
                        Assert.That(psm.Modifications[1].ModLocation, Is.EqualTo(16));
                        Assert.That(psm.Modifications[1].ModifiedResidue, Is.EqualTo('C'));
                        Assert.That(psm.Modifications[1].ModName, Is.EqualTo("Carbamidomethyl"));

                        break;
                    case 1:
                        Assert.That(psm.Modifications.Length, Is.EqualTo(1));
                        Assert.That(psm.Modifications[0].ModLocation, Is.EqualTo(8));
                        Assert.That(psm.Modifications[0].ModifiedResidue, Is.EqualTo('M'));
                        Assert.That(psm.Modifications[0].ModName, Is.EqualTo("Oxidation"));
                        break;
                    case 2:
                        Assert.That(psm.Modifications.Length, Is.EqualTo(0));
                        break;
                    case 3:
                        Assert.That(psm.Modifications.Length, Is.EqualTo(1));
                        Assert.That(psm.Modifications[0].ModLocation, Is.EqualTo(2));
                        Assert.That(psm.Modifications[0].ModifiedResidue, Is.EqualTo('S'));
                        Assert.That(psm.Modifications[0].ModName, Is.EqualTo("Phospho"));
                        break;
                }
            }
        }

        [Test]
        public static void TestModificationsConversion_Peptides()
        {
            var peptides = new ProteomeDiscovererPsmFile(PeptideFilePath);
            peptides.LoadResults();

            for (int i = 0; i < peptides.Count(); i++)
            {
                var peptide = peptides.ElementAt(i);
                switch (i)
                {
                    case 0:
                        Assert.That(peptide.Modifications.Length, Is.EqualTo(0));
                        break;
                    case 1:
                        Assert.That(peptide.Modifications.Length, Is.EqualTo(1));
                        Assert.That(peptide.Modifications[0].ModLocation, Is.EqualTo(8));
                        Assert.That(peptide.Modifications[0].ModifiedResidue, Is.EqualTo('C'));
                        Assert.That(peptide.Modifications[0].ModName, Is.EqualTo("Carbamidomethyl"));
                        break;
                    case 2:
                        Assert.That(peptide.Modifications.Length, Is.EqualTo(1));
                        Assert.That(peptide.Modifications[0].ModLocation, Is.EqualTo(3));
                        Assert.That(peptide.Modifications[0].ModifiedResidue, Is.EqualTo('S'));
                        Assert.That(peptide.Modifications[0].ModName, Is.EqualTo("Phospho"));
                        break;
                }
            }
        }
    }
}
