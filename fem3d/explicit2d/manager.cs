﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class manager : MonoBehaviour
{
    public Material material;
    float[] position_data = new float[18];

    const int WARP_SIZE = 2;

    const int Nx = 2;
    const int Ny = Nx;
    const int node_num = Nx * Nx;
    const int element_num = (Nx - 1) * (Nx - 1) * 2;

    float dx = 1.0f / Nx;

    float[] node_pos_data = new float[node_num * 3];
    int[] element_data = new int[element_num * 3];
    int kernel;

    float[] element_minv_data = new float[element_num * 4];

    public ComputeShader init_cs;
    public ComputeShader computeForce_cs;
    public ComputeShader explicitScheme_cs;

    // 结点属性一律用三个浮点存储，但只使用前两个
    ComputeBuffer node_pos;
    ComputeBuffer node_vel;
    ComputeBuffer node_force;
    ComputeBuffer node_n;
    ComputeBuffer node_normal;
    ComputeBuffer element;
    ComputeBuffer element_minv;
    private void Start()
    {
        node_pos = new ComputeBuffer(node_num * 3, sizeof(float));
        node_vel = new ComputeBuffer(node_num * 3, sizeof(float));
        node_force = new ComputeBuffer(node_num * 3, sizeof(float), ComputeBufferType.Raw);
        node_normal = new ComputeBuffer(node_num * 3, sizeof(float), ComputeBufferType.Raw);
        node_n = new ComputeBuffer(node_num * 3, sizeof(float));
        element = new ComputeBuffer(element_num * 3, sizeof(int));
        element_minv = new ComputeBuffer(element_num * 4, sizeof(float));

        int cnt;

        cnt = 0;
        for (int j = 0; j < Ny; j++)
        {
            for (int i = 0; i < Nx; i++)
            {
                node_pos_data[cnt + 0] = 0;
                node_pos_data[cnt + 1] = 0;
                node_pos_data[cnt + 2] = 0;
                cnt += 3;
            }
        }
        node_force.SetData(node_pos_data);
        node_vel.SetData(node_pos_data);
        cnt = 0;
        for (int j = 0;j  < Ny;j++)
        {
            for(int i = 0;i < Nx;i++)
            {
                node_pos_data[cnt + 0] = i;// * dx;
                node_pos_data[cnt + 1] = j;// * dx;
                node_pos_data[cnt + 2] = 0;
                cnt += 3;
            }
        }
        node_pos.SetData(node_pos_data);



        cnt = 0;
        for (int j = 0; j < (Ny - 1); j++)
        {
            for (int i = 0; i < (Nx - 1); i++)
            {
                int idx0 = j * Nx + i;
                int idx1 = idx0 + 1;
                int idx2 = idx0 + Nx;
                int idx3 = idx2 + 1;
                element_data[cnt + 0] = idx0;
                element_data[cnt + 1] = idx1;
                element_data[cnt + 2] = idx2;

                element_data[cnt + 3] = idx2;
                element_data[cnt + 4] = idx1;
                element_data[cnt + 5] = idx3;
                cnt += 6;
            }
        }
        element.SetData(element_data);

        kernel = init_cs.FindKernel("CSMain");
        init_cs.SetBuffer(kernel, "node_pos", node_pos);
        init_cs.SetBuffer(kernel, "element", element);
        init_cs.SetBuffer(kernel, "element_minv", element_minv);
        init_cs.Dispatch(kernel, element_num / WARP_SIZE, 1, 1);

      //  node_pos_data[0] =  -0.3f;
        node_pos.SetData(node_pos_data);

        kernel = computeForce_cs.FindKernel("CSMain");
        computeForce_cs.SetBuffer(kernel, "node_pos", node_pos);
        computeForce_cs.SetBuffer(kernel, "element", element);
        computeForce_cs.SetBuffer(kernel, "element_minv", element_minv);
        computeForce_cs.SetBuffer(kernel, "node_force", node_force);
        computeForce_cs.SetBuffer(kernel, "node_normal", node_normal);
        computeForce_cs.SetFloat("mu", 1);
        computeForce_cs.SetFloat("la", 1);
        computeForce_cs.SetFloat("dx", 1);
        computeForce_cs.SetFloat("invMass", 0.1f);
        computeForce_cs.Dispatch(kernel, element_num / WARP_SIZE, 1, 1);

        element_minv.GetData(element_minv_data);
        //for (int i = 0; i < element_minv_data.Length; i++) Debug.Log(element_minv_data[i].ToString("f4"));

        //  element_minv.GetData(element_minv_data);
        //    for (int i = 0; i < element_minv_data.Length; i++) Debug.Log(element_minv_data[i].ToString("f4"));



        kernel = explicitScheme_cs.FindKernel("CSMain");
        explicitScheme_cs.SetFloat("mass", 1.0f);
        explicitScheme_cs.SetFloat("dt", 0.01f);
        explicitScheme_cs.SetBuffer(kernel, "node_vel", node_vel);
        explicitScheme_cs.SetBuffer(kernel, "node_pos", node_pos);
        explicitScheme_cs.SetBuffer(kernel, "node_force", node_force);
        explicitScheme_cs.SetBuffer(kernel, "node_normal", node_normal);
        explicitScheme_cs.SetBuffer(kernel, "node_n", node_n);
        explicitScheme_cs.Dispatch(kernel, node_num * 3 / WARP_SIZE, 1, 1);


        node_n.GetData(node_pos_data);
        for (int i = 0; i < node_pos_data.Length; i++) Debug.Log(node_pos_data[i].ToString("f4"));

    }

    private void Update()
    {
        


        
    }
    void OnRenderObject()
    {
        //https://zenn.dev/fuqunaga/scraps/782ea5ca0b002f
        // 花了四个小时，终于找到一个简单的教程了
        material.SetBuffer("position_buffer", node_pos);
        material.SetBuffer("normal_buffer", node_n);
        material.SetBuffer("index_buffer", element);
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 3, element_num);
    }
}
