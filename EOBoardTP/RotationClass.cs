using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotationClass
{
    internal class Rotation
    {
        //    "RotationMatrixScanXUpperX", "RotationMatrixScanXUpperY", "RotationMatrixScanYUpperX", "RotationMatrixScanYUpperY",
        //   "RotationMatrixScanXLowerX", "RotationMatrixScanXLowerY", "RotationMatrixScanYLowerX", "RotationMatrixScanYLowerY",
        //   "ScanGainUpperX",  "ScanGainUpperY",  "ScanGainLowerX", "ScanGainLowerY"

        internal static double[] ScanRateCompensationTable = new double[15]
		// Scan speed
		//         -1            0            1            2            3            4            5
		{ 1.035193755, 1.023222291, 1.015589415, 1.008672397, 1.007066817, 1.003672378, 1.000566817, 
		//        6            7            8            9           10           11           12           13
		1.000000000, 1.000000000, 1.000000000, 1.000000000, 1.000000000, 1.000000000, 1.000000000, 1.000000000 };

        public double L2F { get; set; }
        public double Ortho { get; set; } 
        public double ScanXCoilX { get; set; }
        public double ScanXCoilY { get; set; }
        public double ScanYCoilX { get; set; }
        public double ScanYCoilY { get; set; }

        public double Angle { get; set; }

        internal void CalculateRotation()
        {
            ScanXCoilX = Math.Cos((Angle) / 180 * Math.PI) * 90;
            ScanXCoilY = -Math.Sin((Angle + Ortho) / 180 * Math.PI) * 90;
            ScanYCoilX = Math.Sin((Angle) / 180 * Math.PI) * 90 * L2F;
            ScanYCoilY = Math.Cos((Angle + Ortho) / 180 * Math.PI) * 90 * L2F;
        }

    }
}
