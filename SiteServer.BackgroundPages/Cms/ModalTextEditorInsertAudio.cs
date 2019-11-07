﻿using System;
using System.Collections;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using SiteServer.Utils;
using SiteServer.CMS.Core;
using SiteServer.Utils.LitJson;

namespace SiteServer.BackgroundPages.Cms
{
    public class ModalTextEditorInsertAudio : BasePageCms
    {
        public TextBox TbPlayUrl;
        public CheckBox CbIsAutoPlay;

        private string _attributeName;

        public static string GetOpenWindowString(int siteId, string attributeName)
        {
            return LayerUtils.GetOpenScript("插入音频", PageUtils.GetCmsUrl(siteId, nameof(ModalTextEditorInsertAudio), new NameValueCollection
            {
                {"AttributeName", attributeName}
            }), 600, 400);
        }

        public string UploadUrl => PageUtils.GetCmsUrl(SiteId, nameof(ModalTextEditorInsertAudio), new NameValueCollection
        {
            {"upload", true.ToString()}
        });

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            if (AuthRequest.IsQueryExists("upload"))
            {
                var json = JsonMapper.ToJson(Upload());
                Response.Write(json);
                Response.End();
                return;
            }

            _attributeName = AuthRequest.GetQueryString("AttributeName");

            if (IsPostBack) return;

            CbIsAutoPlay.Checked = Site.Additional.ConfigUEditorAudioIsAutoPlay;
        }

        public string TypeCollection => Site.Additional.VideoUploadTypeCollection;

        private Hashtable Upload()
        {
            var success = false;
            var playUrl = string.Empty;
            var message = "音频上传失败";

            if (Request.Files["filedata"] != null)
            {
                var postedFile = Request.Files["filedata"];
                try
                {
                    if (!string.IsNullOrEmpty(postedFile?.FileName))
                    {
                        var filePath = postedFile.FileName;
                        var fileExtName = PathUtils.GetExtension(filePath);

                        var isAllow = true;
                        if (!PathUtility.IsVideoExtenstionAllowed(Site, fileExtName))
                        {
                            message = "此格式不允许上传，请选择有效的音频文件";
                            isAllow = false;
                        }
                        if (!PathUtility.IsVideoSizeAllowed(Site, postedFile.ContentLength))
                        {
                            message = "上传失败，上传文件超出规定文件大小";
                            isAllow = false;
                        }

                        if (isAllow)
                        {
                            var localDirectoryPath = PathUtility.GetUploadDirectoryPath(Site, fileExtName);
                            var localFileName = PathUtility.GetUploadFileName(Site, filePath);
                            var localFilePath = PathUtils.Combine(localDirectoryPath, localFileName);

                            postedFile.SaveAs(localFilePath);
                            playUrl = PageUtility.GetSiteUrlByPhysicalPathAsync(Site, localFilePath, true).GetAwaiter().GetResult();
                            playUrl = PageUtility.GetVirtualUrl(Site, playUrl);
                            success = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }

            var jsonAttributes = new Hashtable();
            if (success)
            {
                jsonAttributes.Add("success", "true");
                jsonAttributes.Add("playUrl", playUrl);
            }
            else
            {
                jsonAttributes.Add("success", "false");
                jsonAttributes.Add("message", message);
            }

            return jsonAttributes;
        }

        public override void Submit_OnClick(object sender, EventArgs e)
        {
            var playUrl = TbPlayUrl.Text;
            var isAutoPlay = CbIsAutoPlay.Checked;

            if (isAutoPlay != Site.Additional.ConfigUEditorAudioIsAutoPlay)
            {
                Site.Additional.ConfigUEditorAudioIsAutoPlay = isAutoPlay;
                DataProvider.SiteDao.UpdateAsync(Site).GetAwaiter().GetResult();
            }

            var script = "parent." + UEditorUtils.GetInsertAudioScript(_attributeName, playUrl, Site);
            LayerUtils.CloseWithoutRefresh(Page, script);
        }

    }
}
