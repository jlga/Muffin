﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using GAF;
using GAF.Operators;

namespace CurveFittingGeneticAlgorithm
{
    class Genetics
    {
        const double crossoverProbability = 0.85;
        const double mutationProbability = 0.08;
        const int    elitismPercentage = 1;
        const int    populationSize = 200;

        double lastGenFitness = 0;
        Dictionary<int, double> dicOG;

        GeneticAlgorithm ga;
        Main form;
        Func<string,int,string,bool> pop;

        public Genetics(Main form1, Func<string, int, string, bool> populateM)
        {
            pop = populateM;
            form1.Text = "test";
            form = form1;

            var population = new Population(populationSize, 576, false, false);

            //create the genetic operators 
            var elite = new Elite(elitismPercentage);

            var crossover = new Crossover(crossoverProbability, true)
            {
                CrossoverType = CrossoverType.SinglePoint
            };

            var mutation = new BinaryMutate(mutationProbability, true);

            //create the GA itself 
            ga = new GeneticAlgorithm(population, EvaluateFitness);

            ga.OnGenerationComplete += ga_OnGenerationComplete;

            ga.Operators.Add(elite);
            ga.Operators.Add(crossover);
            ga.Operators.Add(mutation);
        }

        public void run()
        {
            ga.Run(TerminateAlgorithm);
        }

        public static bool TerminateAlgorithm(Population population, int currentGeneration, long currentEvaluation)
        {
            return population.MaximumFitness == 1;
        }

        public double EvaluateFitness(Chromosome chromosome)
        {
            int numOfBytes = chromosome.ToBinaryString().Length / 8;
            byte[] bytes = new byte[numOfBytes];
            for(int i=0; i<numOfBytes; i++)
            {
                bytes[i] = Convert.ToByte(chromosome.ToBinaryString().Substring(8 * i, 8), 2);
            }
            if (dicOG == null)
            {
               dicOG = Utils.convertToDictionary(new SmallEquation(0,0,0,1,0), 20);
            }
            Dictionary<int, double> dicFK = Utils.convertToDictionary(Decoder.decodeToEquation(bytes), 20);
            double error = Utils.calculateError(dicOG, dicFK);
            double calcerror = 1 - ((0.00001 * error) / ((0.00001 * error) + 1));
            return calcerror;
        }

        private void ga_OnGenerationComplete(object sender, GaEventArgs e)
        {
            Console.WriteLine("---Generation Complete---");
            Console.WriteLine("Gen: " + e.Generation);
            
            Console.WriteLine("Fit: " + e.Population.MaximumFitness + " " + (e.Population.MaximumFitness-lastGenFitness));
            lastGenFitness = e.Population.MaximumFitness;

            //get the best solution 
            var chromosome = e.Population.GetTop(1)[0];

            int numOfBytes = chromosome.ToBinaryString().Length / 8;
            byte[] bytes = new byte[numOfBytes];
            for (int i = 0; i < numOfBytes; i++)
            {
                bytes[i] = Convert.ToByte(chromosome.ToBinaryString().Substring(8 * i, 8), 2);
            }
            Equation eq = Decoder.decodeToEquation(bytes);
            eq.fitness = e.Population.MaximumFitness;
            Console.WriteLine("Eq : " + eq.ToString());
            form.backgroundWorker1.ReportProgress(Convert.ToInt32(e.Population.MaximumFitness*100), JsonConvert.SerializeObject(eq));
        }
    }
}
