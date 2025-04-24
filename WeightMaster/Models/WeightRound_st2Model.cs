using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightMaster.Models
{
    public class WeightRound_st2Model
    {
        public float weightScalerWeight { get; set; }
        public int acceptedSackWeight { get; set; }

        public int currentAcceptedLeafWeight { get; set; }
        public double currentAcceptedGoldenWeight { get; set; }
        public double currentAcceptedNormalLeafWeight { get; set; }


        //public int availableAcceptedLeafWeight { get; set; }
        public int availableGoldenLeafWeight { get; set; }
        public int availableNormalLeafWeight { get; set; }

        public int wateredWeight { get; set; }
        public int maturedWeight { get; set; }
        public int spoiledWeight { get; set; }
        public int rejectedWeight { get; set; }

        public double currentTotalDeduction { get; set; }


        public WeightRound_st2Model(
            float weightScalerWeight,
            int acceptedSackWeight, 
            double currentGoldenWeight, 
            double currentNormalLeafWeight, 
            int availableGoldenLeafWeight, 
            int availableNormalLeafWeight, 
            int wateredWeight, 
            int maturedWeight, 
            int spoiledWeight, 
            int rejectedWeight
            )
        {
            this.acceptedSackWeight = acceptedSackWeight;

            this.currentAcceptedGoldenWeight = currentGoldenWeight;
            this.currentAcceptedNormalLeafWeight = currentNormalLeafWeight;

            this.availableGoldenLeafWeight = availableGoldenLeafWeight;
            this.availableNormalLeafWeight = availableNormalLeafWeight;

            this.wateredWeight = wateredWeight;
            this.maturedWeight = maturedWeight;
            this.spoiledWeight = spoiledWeight;
            this.rejectedWeight = rejectedWeight;

            this.currentTotalDeduction = wateredWeight + maturedWeight + spoiledWeight + rejectedWeight;
        }

        public override string ToString() =>
            $"test";
    }
}
