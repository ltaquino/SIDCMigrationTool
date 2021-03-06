using SOFOS2_Migration_Tool.Payment.Model;
using SOFOS2_Migration_Tool.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOFOS2_Migration_Tool.Payment.Controller
{
    class OfficialReceiptController
    {
        Global g = null;
        public List<OfficialReceipt> GetOfficialReceiptHeader(string date, string transprefix)
        {
            try
            {
                var result = new List<OfficialReceipt>();
               

                var filter = new Dictionary<string, object>()
                {
                    { "@transdate", date },
                    { "@transprefix", transprefix }
                };

                using (var conn = new MySQLHelper(Global.SourceDatabase, PaymentQuery.GetQuery(payment.ORHeader), filter))
                {
                    using (var dr = conn.MySQLReader())
                    {
                        while (dr.Read())
                        {
                            result.Add(new OfficialReceipt
                            {
                                Reference = dr["reference"].ToString(),
                                Total = Convert.ToDecimal(dr["total"]),
                                TransDate = dr["transDate"].ToString(),
                                IdUser = dr["idUser"].ToString(),
                                MemberId = dr["memberId"].ToString(),
                                MemberName = dr["memberName"].ToString(),
                                Status = dr["status"].ToString(),
                                Remarks = dr["remarks"].ToString(),
                                Cancelled = Convert.ToBoolean(dr["cancelled"]),
                                sType = dr["type"].ToString(),
                                PaidBy = dr["paidBy"].ToString(),
                                BranchCode = Global.BranchCode,
                                Extracted = dr["extracted"].ToString(),
                                TransType = dr["transType"].ToString(),
                                RefTransType = dr["refTransType"].ToString()
                            });

                            g = new Global();
                            result.Where(c => c.MemberId == dr["memberId"].ToString()).Select(c => { c.MemberName = g.GetMemberName(dr["memberId"].ToString()); return c; }).ToList();
                            result.Where(c => c.MemberId == dr["memberId"].ToString()).Select(c => { c.AccountNumber = g.GetAccountNumber(dr["memberId"].ToString(), dr["refTransType"].ToString()); return c; }).ToList();
                        }
                    }
                }

                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<OfficialReceipt> GetOfficialReceiptDetail(string date, string transprefix)
        {
            try
            {
                var result = new List<OfficialReceipt>();



                var filter = new Dictionary<string, object>()
                {
                    { "@transdate", date },
                    { "@transprefix", transprefix }
                };

                using (var conn = new MySQLHelper(Global.SourceDatabase, PaymentQuery.GetQuery(payment.ORDetail), filter))
                {
                    using (var dr = conn.MySQLReader())
                    {
                        while (dr.Read())
                        {
                            result.Add(new OfficialReceipt
                            {
                                Amount = Convert.ToDecimal(dr["amount"]),
                                IdUser = dr["idUser"].ToString(),
                                AccountCode = dr["accountCode"].ToString(),
                                pType = dr["pType"].ToString(),
                                Reference = dr["reference"].ToString(),
                            });
                            g = new Global();
                          
                        }
                    }
                }

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void InsertOR(List<OfficialReceipt> _header, List<OfficialReceipt> _detail)
        {
            try
            {
                Global g = new Global();
                int transNum = 0;
                long series = 0;

                using (var conn = new MySQLHelper(Global.DestinationDatabase))
                {
                    conn.BeginTransaction();

                    transNum = g.GetLatestTransNum("fp000", "transNum");

                    foreach (var item in _header)
                    {
                        
                        var param = new Dictionary<string, object>()
                        {
                            { "@transNum", transNum },
                            { "@reference", item.Reference},
                            { "@Total", item.Total },
                            { "@transDate", item.TransDate },
                            { "@idUser", item.IdUser },
                            { "@memberId", item.MemberId },
                            { "@memberName", item.MemberName },
                            { "@status", item.Status },
                            { "@cancelled", item.Cancelled },
                            { "@remarks", item.Remarks },
                            { "@type", item.sType },
                            { "@accountCode", item.AccountCode },
                            { "@paidBy", item.PaidBy },
                            { "@branchCode", Global.BranchCode },
                            { "@extracted", item.Extracted },
                            { "@transType", item.TransType },
                            { "@series", "" },
                            { "@AccountNo", item.AccountNumber },
                            { "@refTransType", item.RefTransType }
                        };

                        conn.ArgSQLCommand = PaymentQuery.InsertQuery(payment.ORHeader);
                        conn.ArgSQLParam = param;

                        //Execute insert header
                        conn.ExecuteMySQL();

                        #region Insert Details
                        var details = _detail.Where(n => n.Reference == item.Reference).ToList();

                        foreach (var detail in details)
                        {
                            var detailParam = new Dictionary<string, object>()
                                {
                                    {"@transNum", transNum },
                                    {"@crossReference", "" },
                                    {"@amount", detail.Amount },
                                    {"@idUser", detail.IdUser },
                                    {"@balance", detail.Balance },
                                    {"@accountCode", detail.AccountCode },
                                    {"@pType", detail.pType },
                                    {"@accountName", detail.AccountName },
                                    {"@refTransType", detail.DetRefTransType }
                                };

                            conn.ArgSQLCommand = PaymentQuery.InsertQuery(payment.ORDetail);
                            conn.ArgSQLParam = detailParam;

                            //execute insert detail
                            var cmdDetail = conn.ExecuteMySQL();
                        }
                        #endregion

                        transNum++;
                       
                    }

                    conn.ArgSQLCommand = Query.UpdateReferenceCount();
                    conn.ArgSQLParam = new Dictionary<string, object>() { { "@series", series - 1 }, { "@transtype", "OR" } };
                    conn.ExecuteMySQL();


                    conn.CommitTransaction();
                }

            }
            catch
            {

                throw;
            }
        }
    }
}
