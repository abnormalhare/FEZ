using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace FezGame.Tools;

public class ErrorDialog : Form
{
	private IContainer components;

	private Label label1;

	private LinkLabel linkLabel1;

	private Label label2;

	private LinkLabel linkLabel2;

	private Button button1;

	public ErrorDialog()
	{
		InitializeComponent();
	}

	private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		Process.Start("https://getsatisfaction.com/polytron/topics/support_for_intel_integrated_graphics_hardware");
	}

	private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		Process.Start("http://polytroncorporation.com/support/");
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FezGame.Tools.ErrorDialog));
		this.label1 = new System.Windows.Forms.Label();
		this.linkLabel1 = new System.Windows.Forms.LinkLabel();
		this.label2 = new System.Windows.Forms.Label();
		this.linkLabel2 = new System.Windows.Forms.LinkLabel();
		this.button1 = new System.Windows.Forms.Button();
		base.SuspendLayout();
		this.label1.Location = new System.Drawing.Point(12, 9);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(512, 189);
		this.label1.TabIndex = 0;
		this.label1.Text = resources.GetString("label1.Text");
		this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.linkLabel1.AutoSize = true;
		this.linkLabel1.Location = new System.Drawing.Point(159, 190);
		this.linkLabel1.Name = "linkLabel1";
		this.linkLabel1.Size = new System.Drawing.Size(218, 13);
		this.linkLabel1.TabIndex = 2;
		this.linkLabel1.TabStop = true;
		this.linkLabel1.Text = "http://polytroncorporation.com/support/";
		this.linkLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(linkLabel1_LinkClicked);
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(150, 149);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(0, 13);
		this.label2.TabIndex = 2;
		this.linkLabel2.AutoSize = true;
		this.linkLabel2.Location = new System.Drawing.Point(28, 119);
		this.linkLabel2.Name = "linkLabel2";
		this.linkLabel2.Size = new System.Drawing.Size(483, 13);
		this.linkLabel2.TabIndex = 3;
		this.linkLabel2.TabStop = true;
		this.linkLabel2.Text = "https://getsatisfaction.com/polytron/topics/support_for_intel_integrated_graphics_hardware";
		this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(linkLabel2_LinkClicked);
		this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.button1.Location = new System.Drawing.Point(231, 228);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(75, 23);
		this.button1.TabIndex = 1;
		this.button1.Text = "Close";
		this.button1.UseVisualStyleBackColor = true;
		base.AcceptButton = this.button1;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.CancelButton = this.button1;
		base.ClientSize = new System.Drawing.Size(536, 263);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.linkLabel2);
		base.Controls.Add(this.linkLabel1);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.label1);
		this.Font = new System.Drawing.Font("Segoe UI", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "ErrorDialog";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "FEZ - Fatal Error";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
