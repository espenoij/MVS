using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS.Services;

namespace MVSTests.Services
{
    /// <summary>
    /// Tests for the acceptance-criteria grading added to
    /// <see cref="VerificationAssessment"/> for the compliance section of the
    /// verification report.
    /// </summary>
    [TestClass]
    public class VerificationAssessmentComplianceTests
    {
        [TestMethod]
        public void EvaluateCompliance_WithinThreshold_IsPass()
        {
            Assert.AreEqual(ComplianceResult.Pass, VerificationAssessment.EvaluateCompliance(0.30, 0.50));
            Assert.AreEqual(ComplianceResult.Pass, VerificationAssessment.EvaluateCompliance(-0.50, 0.50));
        }

        [TestMethod]
        public void EvaluateCompliance_WithinConditionalMargin_IsConditional()
        {
            // 0.50 < |0.60| <= 0.75 (1.5x)
            Assert.AreEqual(ComplianceResult.Conditional, VerificationAssessment.EvaluateCompliance(0.60, 0.50));
            Assert.AreEqual(ComplianceResult.Conditional, VerificationAssessment.EvaluateCompliance(-0.75, 0.50));
        }

        [TestMethod]
        public void EvaluateCompliance_BeyondConditionalMargin_IsFail()
        {
            Assert.AreEqual(ComplianceResult.Fail, VerificationAssessment.EvaluateCompliance(0.80, 0.50));
        }

        [TestMethod]
        public void EvaluateCompliance_NoOrInvalidCriterion_IsNotAssessed()
        {
            Assert.AreEqual(ComplianceResult.NotAssessed, VerificationAssessment.EvaluateCompliance(0.30, null));
            Assert.AreEqual(ComplianceResult.NotAssessed, VerificationAssessment.EvaluateCompliance(0.30, 0.0));
            Assert.AreEqual(ComplianceResult.NotAssessed, VerificationAssessment.EvaluateCompliance(0.30, -1.0));
        }

        [TestMethod]
        public void EvaluateCompliance_NaNDeviation_IsNotAssessed()
        {
            Assert.AreEqual(ComplianceResult.NotAssessed, VerificationAssessment.EvaluateCompliance(double.NaN, 0.50));
        }

        [TestMethod]
        public void ComplianceLabel_ReturnsReadableText()
        {
            Assert.AreEqual("Pass", VerificationAssessment.ComplianceLabel(ComplianceResult.Pass));
            Assert.AreEqual("Conditional Pass", VerificationAssessment.ComplianceLabel(ComplianceResult.Conditional));
            Assert.AreEqual("Fail", VerificationAssessment.ComplianceLabel(ComplianceResult.Fail));
            Assert.AreEqual("Not assessed", VerificationAssessment.ComplianceLabel(ComplianceResult.NotAssessed));
        }
    }
}
