using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AiEvolver : MonoBehaviour
{
    [Header("Setup settings")]
    [SerializeField, Range(1, 100)] private float timeScale;

    [Header("Saving")]
    [SerializeField] private bool saveBestBrain;

    [Header("Evulution Settings")]
    [SerializeField] private bool keepBest;
    [SerializeField] private bool isSpliceBased;
    [SerializeField] private float randomMutationRange;
    [SerializeField, Range(1, 2)] private int spliceCount;
    [SerializeField, Range(0, 100)] private int uniformProbability;
    [SerializeField, Range(0, 100)] private int dominantGenomePresistancyProbaility;
    [SerializeField, Range(0, 100)] private int mutationChance;


    public List<PokerGameScript> games { get; private set; }
    private Network traningNetwork;
    private int generationCount;
    private int genomVariationCount;

    private void Start() {
        games = GetComponentsInChildren<PokerGameScript>().ToList();

        if(games != null)
            StartCoroutine(CheckGames() as IEnumerator);
        else
            Debug.LogError("games not found");
    }

    private void OnApplicationQuit() {
        if(saveBestBrain) {

            float fittnes = 0;
            PokerPlayer player = null;

            foreach(var game in games) {
                if(fittnes < game.GetBestPlayer().GetFitness()) {
                    fittnes = game.GetBestPlayer().GetFitness();
                    player = game.GetBestPlayer();
                }
            }

            player.SaveBrain();
            Debug.Log("Best brain saved");
        }
    }

    public IEnumerator CheckGames() {
        while(true) {
            yield return new WaitForSeconds(1);

            int counter = 0;

            foreach(var game in games) {
                if(game.gameDone)
                    counter++;
            }

            //if all games are done make a new generation
            if(counter >= games.Count()) {
                GenerateNewGeneration();
                generationCount++;
                Debug.Log("Generation Reset, gen count = " + generationCount);
            }
        }
    }

    private void GenerateNewGeneration() {

        float sum = 0;
        List<PokerPlayer> bestPlayers = new List<PokerPlayer>();

        //get all the best players from each table and sum ther fittnes
        foreach(var game in games) {
            bestPlayers.Add(game.GetBestPlayer());
            sum += game.GetBestPlayer().GetFitness();
        }

        Debug.Log("avrage fitness: " + sum / bestPlayers.Count);

        //new list to keep all new genoms
        List<float[]> newGenoms = new List<float[]>();

        //generate all new genoms
        foreach(var game in games) {
            foreach(var player in game.players) {
                newGenoms.Add(GetEvolvedNetworkGenom(sum, ref bestPlayers));
            } 
        }


        //overide old networks
        if(keepBest) {
            PokerPlayer bestPlayer = bestPlayers[0];
            float currentFitness = 0;
            float worstFittness = bestPlayer.GetFitness();

            //find the best car and worst car
            foreach(var player in bestPlayers) {
                if(player.GetFitness() > currentFitness) {
                    currentFitness = player.GetFitness();
                    bestPlayer = player;
                }

                if(player.GetFitness() < worstFittness)
                    worstFittness = player.GetFitness();
            }

            //mutate best car
            bestPlayer.network.SetNetworkByGenom(MutateGenom(bestPlayer.network.GetNetworkGenom()));

            Debug.Log("best fitness: " + currentFitness);
            Debug.Log("worst fitness: " + worstFittness);

            for(int i = 0; i < games.Count; i++) {
                for(int j = 0; j < games[i].players.Length; j++) {
                    if(games[i].players[j] != bestPlayer)
                        games[i].players[j].network.SetNetworkByGenom(newGenoms[i + j]);
                }
                games[i].ResetPlayers();
            }

        } else {

            //overwrite old genoms with new
            for(int i = 0; i < games.Count; i++) {
                for(int j = 0; j < games[i].players.Length; j++) {
                    games[i].players[j].network.SetNetworkByGenom(newGenoms[i + j]);
                }
                games[i].ResetPlayers();
            }
        }

        //start all games again
        foreach(var game in games)
            game.ResetGame();
    }

    private float[] GetEvolvedNetworkGenom(float sum, ref List<PokerPlayer> bestPlayers) {
        //temp vars
        PokerPlayer select1;
        PokerPlayer select2;

        select1 = SelectCar(sum, ref bestPlayers);
        select2 = SelectCar(sum, ref bestPlayers);

        //decide which car is dominat based on fitness and which gene combination method to use
        float[][] finalGenoms;
        if(select1.GetFitness() > select2.GetFitness())
            finalGenoms = isSpliceBased ? PointSpliceCrossover(select1, select2) : UniformSpliceCrossover(select1, select2);
        else
            finalGenoms = isSpliceBased ? PointSpliceCrossover(select2, select1) : UniformSpliceCrossover(select2, select1);

        //do mutation with x% chance
        for(int i = 0; i < finalGenoms.Length; i++) {
            finalGenoms[i] = MutateGenom(finalGenoms[i]);
        }

        // return dominat genom with % based chanse
        return Random.Range(0, 100) >= dominantGenomePresistancyProbaility ? finalGenoms[1] : finalGenoms[0];
    }

    private PokerPlayer SelectCar(float sum, ref List<PokerPlayer> bestPlayers) {
        float rand = Random.Range(0, sum);
        float counter = 0;

        foreach(var player in bestPlayers) {
            if(counter < rand) {
                counter += player.GetFitness();
            } else {
                return player;
            }
        }

        return bestPlayers[0];
    }

    private float[][] PointSpliceCrossover(PokerPlayer dominat, PokerPlayer submissive) {

        float[][] returnList = new float[2][];
        float[] dominatGenom = dominat.network.GetNetworkGenom();
        float[] submissiveGenom = submissive.network.GetNetworkGenom();


        if(spliceCount > 1) {
            //get a random split point
            int rand = Random.Range(0, dominatGenom.Length - 1);

            for(int i = 0; i < dominatGenom.Length; i++) {
                if(i > rand) {
                    //sawp genom values to the rigth of the splitting point
                    float holder = dominatGenom[i];
                    dominatGenom[i] = submissiveGenom[i];
                    submissiveGenom[i] = holder;
                }
            }

        } else {

            //get 2 split points, one random and the other one based on what is left
            int rand = Random.Range(0, dominatGenom.Length - 3);
            int rand2 = Random.Range(rand + 2, dominatGenom.Length - 1);

            for(int i = 0; i < dominatGenom.Length; i++) {
                if(i > rand && i < rand2) {
                    //sawp genom values in between the 2 random generated values
                    float holder = dominatGenom[i];
                    dominatGenom[i] = submissiveGenom[i];
                    submissiveGenom[i] = holder;
                }
            }
        }

        returnList[0] = dominatGenom;
        returnList[1] = submissiveGenom;
        return returnList;
    }

    private float[][] UniformSpliceCrossover(PokerPlayer dominat, PokerPlayer submissive) {

        float[][] returnList = new float[2][];
        float[] dominatGenom = dominat.network.GetNetworkGenom();
        float[] submisive = submissive.network.GetNetworkGenom();

        //do the combination, works for 1 and 2 splits points
        for(int i = 0; i < dominatGenom.Length; i++) {
            if(Random.Range(0, 100) >= uniformProbability) {
                //sawp genom values in a given range
                float holder = dominatGenom[i];
                dominatGenom[i] = submisive[i];
                submisive[i] = holder;
            }
        }

        returnList[0] = dominatGenom;
        returnList[1] = submisive;
        return returnList;
    }

    private float[] MutateGenom(float[] genom) {
        for(int i = 0; i < genom.Length; i++) {
            if(Random.Range(0, 100) < mutationChance) {
                genom[i] += Random.Range(-randomMutationRange, randomMutationRange);
            }
        }
        return genom;
    }

    private void QuickSort(List<PokerPlayer> arr, int start, int end) {
        int i;
        if(start < end) {
            i = Partition(arr, start, end);

            QuickSort(arr, start, i - 1);
            QuickSort(arr, i + 1, end);
        }
    }

    private int Partition(List<PokerPlayer> arr, int start, int end) {
        PokerPlayer temp;
        PokerPlayer p = arr[end];
        int i = start - 1;

        for(int j = start; j <= end - 1; j++) {
            if(arr[j].GetFitness() <= p.GetFitness()) {
                i++;
                temp = arr[i];
                arr[i] = arr[j];
                arr[j] = temp;
            }
        }

        temp = arr[i + 1];
        arr[i + 1] = arr[end];
        arr[end] = temp;
        return i + 1;
    }

    private void OnValidate() {
        Time.timeScale = timeScale;
    }
}
