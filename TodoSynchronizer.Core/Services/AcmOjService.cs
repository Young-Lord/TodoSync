using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using TodoSynchronizer.Core.Models;
using TodoSynchronizer.Core.Models.AcmOjModels;
using TodoSynchronizer.Core.Service;

namespace TodoSynchronizer.Core.Services
{
    public class AcmOjService
    {
        public static string Token { get; set; }
        public static bool IsLogin { get; set; }
        public static HttpClient Client { get; set; }

        public static CommonResult Login(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return new CommonResult(false, "ACMOJ Token 为空");

            Client = new HttpClient()
            {
                BaseAddress = new Uri("https://acm.sjtu.edu.cn/OnlineJudge/api/v1/")
            };
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            IsLogin = true;
            Token = token;
            return new CommonResult(true, "登录成功");
        }

        public static List<AcmOjProblemset> ListProblemsets()
        {
            var res = Web.Get(Client, "user/problemsets");
            if (!res.success)
                throw new Exception(res.message);

            if (res.code != HttpStatusCode.OK)
                throw new Exception($"ACMOJ 接口返回错误：{(int)res.code} {res.code}");

            try
            {
                var json = JsonConvert.DeserializeObject<AcmOjProblemsetListResponse>(res.result);
                return json?.Problemsets;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
