using UnityEngine;

public static class MathFunctions {
    public static float Sigmoid(float x) {
        return 1 / (1 + Mathf.Exp(-x));
    }

    public static float ModifiedSigmod(float x) {
        return (2 / (1 + Mathf.Exp(-x))) - 1;
    }

    public static Vector2 RotateVector(Vector2 vector, float degree) {
        float rad = degree * Mathf.Deg2Rad;
        return new Vector2(vector.x * Mathf.Cos(rad) - vector.y * Mathf.Sin(rad), vector.x * Mathf.Sin(rad) + vector.y * Mathf.Cos(rad));
    }
}

public class Matrix {
    private float[][] matrix;

    public Matrix(int width, int hight) {
        matrix = new float[width][];                        //set widht of matrix
        for(int i = 0; i < matrix.Length; i++) {
            matrix[i] = new float[hight];                   //set hight of matrix

            for(int j = 0; j < matrix[i].Length; j++) {
                matrix[i][j] = 0;                           //set all values in the matrix to 0
            }
        }

    }

    public float GetValue(int width, int hight) {
        return matrix[width][hight];
    }

    public void SetValue(int width, int hight, float value) {
        matrix[width][hight] = value;
    }

    public int GetWidth() {
        return matrix.Length;
    }

    public int GetHight() {
        return matrix[0].Length;
    }
}

public class Network {

    public int[] networkStructure { get; private set; }
    private Layer[] layers;

    public Network(int[] layersLayout) {

        layers = new Layer[layersLayout.Length];

        //copy arr to network for save/load function
        networkStructure = new int[layersLayout.Length];
        for(int i = 0; i < networkStructure.Length; i++) {
            networkStructure[i] = layersLayout[i];
        }

        for(int i = 0; i < layers.Length; i++) {
            if(i + 1 < layers.Length) {
                layers[i] = new Layer(layersLayout[i] + 1, layersLayout[i + 1]);    //+1 to width for bias neuron 
                layers[i].neurons[layers[i].neurons.Length - 1].activation = 1;     //set bias neuron to 1
            } else
                layers[i] = new Layer(layersLayout[i], 0);                          //set hight to 0 beacuse it is the last layer so there is no more connections
        }
    }

    public float[] GetNetworkGenom() {
        //calculate the length needed for the array by checking all the weight matrixes
        int len = 0;
        for(int i = 0; i < layers.Length; i++) {
            len += layers[i].wieghtMatrix.GetWidth() * layers[i].wieghtMatrix.GetHight();
        }

        float[] genom = new float[len];

        int layerPointer = 0;


        for(int i = 0; i < layers.Length; i++) {
            int neuronPointer = 0;

            for(int j = 0; j < layers[i].wieghtMatrix.GetHight(); j++) {
                for(int n = 0; n < layers[i].wieghtMatrix.GetWidth(); n++) {
                    genom[layerPointer + neuronPointer] = layers[i].wieghtMatrix.GetValue(n, j);
                    neuronPointer++;
                }
            }
            layerPointer += neuronPointer;
        }

        return genom;
    }

    public void SetNetworkByGenom(float[] genom) {

        int layerPointer = 0;

        for(int i = 0; i < layers.Length; i++) {

            int neuronPointer = 0;

            for(int j = 0; j < layers[i].wieghtMatrix.GetHight(); j++) {
                for(int n = 0; n < layers[i].wieghtMatrix.GetWidth(); n++) {
                    layers[i].wieghtMatrix.SetValue(n, j, genom[layerPointer + neuronPointer]);
                    neuronPointer++;
                }
            }
            layerPointer += neuronPointer;
        }
    }

    public float[] SendInputs(float[] inputs) {
        //set input neurons
        for(int i = 0; i < inputs.Length; i++) {
            layers[0].neurons[i].activation = inputs[i];
        }

        //set activation in next layers neurons
        for(int i = 0; i < layers.Length; i++) {
            if(i + 1 < layers.Length) {
                if(i < layers.Length - 2) {
                    for(int j = 0; j < layers[i + 1].neurons.Length - 1; j++) {                                                 // -1 to exclude the bias neuron
                        layers[i + 1].neurons[j].activation = CalculateActivation(layers[i].wieghtMatrix, layers[i], j);        //use wiegth matrix to set activation in next layer, if there is a next layer
                    }
                } else {
                    for(int j = 0; j < layers[i + 1].neurons.Length; j++) {                                                     // include all so that all outputs counts
                        layers[i + 1].neurons[j].activation = CalculateActivation(layers[i].wieghtMatrix, layers[i], j);        //use wiegth matrix to set activation in next layer, if there is a next layer
                    }
                }
            }

        }

        //create float array with output values
        float[] outputs = new float[layers[layers.Length - 1].neurons.Length];

        for(int i = 0; i < layers[layers.Length - 1].neurons.Length; i++) {
            outputs[i] = layers[layers.Length - 1].neurons[i].activation;
        }

        return outputs;
    }

    private float CalculateActivation(Matrix weightMatrix, Layer currentLayer, int nextLayerNeuron) {
        float sum = 0;

        for(int i = 0; i < currentLayer.neurons.Length; i++) {
            sum += (weightMatrix.GetValue(i, nextLayerNeuron) * currentLayer.neurons[i].activation);        // sum of all weights assosietet with next neuron
        }

        return MathFunctions.Sigmoid(sum);
    }

    public void PrintNetworkIO() {
        string output = "input layer: ";
        for(int i = 0; i < layers[0].neurons.Length; i++) {
            output += " neuron " + i + " a= " + layers[0].neurons[i].activation;
        }
        Debug.Log(output);

        output = "output layer: ";
        for(int i = 0; i < layers[layers.Length - 1].neurons.Length; i++) {
            output += " neuron " + i + " a= " + layers[layers.Length - 1].neurons[i].activation;
        }
        Debug.Log(output);
    }

    public void PrintNetowrkLayout() {
        string output = "";
        for(int i = 0; i < networkStructure.Length; i++) {
            output += "L" + i + " length = " + networkStructure[i];
        }
        Debug.Log(output);
    }
}

public class Layer {

    public Matrix wieghtMatrix;

    public Neuron[] neurons;
    //public Neuron biasNeuron;

    public Layer(int _neurons, int nextNeurons) {
        neurons = new Neuron[_neurons];

        for(int i = 0; i < neurons.Length; i++) {
            neurons[i] = new Neuron(0);
        }

        wieghtMatrix = new Matrix(_neurons, nextNeurons);
        GenerateRandomWeights();
    }

    private void GenerateRandomWeights() {
        for(int i = 0; i < wieghtMatrix.GetWidth(); i++) {
            for(int j = 0; j < wieghtMatrix.GetHight(); j++) {
                float rand = Random.Range(-1.5f, 1.5f);
                wieghtMatrix.SetValue(i, j, rand);
            }
        }
    }
}

public class Neuron {

    public float activation;

    public Neuron(float _activation) {
        activation = _activation;
    }
}

