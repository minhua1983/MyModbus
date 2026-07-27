namespace MyModbus.UI
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.btn_connect = new System.Windows.Forms.Button();
            this.btn_send = new System.Windows.Forms.Button();
            this.lv_message = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lbl_temperature = new System.Windows.Forms.Label();
            this.lbl_humidity = new System.Windows.Forms.Label();
            this.btn_temperature_update = new System.Windows.Forms.Button();
            this.tb_temperature = new System.Windows.Forms.TextBox();
            this.tb_humidity = new System.Windows.Forms.TextBox();
            this.btn_humidity_update = new System.Windows.Forms.Button();
            this.btn_whether_to_collect = new System.Windows.Forms.Button();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.label1 = new System.Windows.Forms.Label();
            this.lbl_collected_count = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lbl_inserted_count = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_connect
            // 
            this.btn_connect.Location = new System.Drawing.Point(13, 13);
            this.btn_connect.Name = "btn_connect";
            this.btn_connect.Size = new System.Drawing.Size(120, 40);
            this.btn_connect.TabIndex = 0;
            this.btn_connect.Text = "开启设备";
            this.btn_connect.UseVisualStyleBackColor = true;
            this.btn_connect.Click += new System.EventHandler(this.btn_connect_Click);
            // 
            // btn_send
            // 
            this.btn_send.Location = new System.Drawing.Point(13, 149);
            this.btn_send.Name = "btn_send";
            this.btn_send.Size = new System.Drawing.Size(100, 25);
            this.btn_send.TabIndex = 1;
            this.btn_send.Text = "发送测试";
            this.btn_send.UseVisualStyleBackColor = true;
            this.btn_send.Click += new System.EventHandler(this.btn_send_Click);
            // 
            // lv_message
            // 
            this.lv_message.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.lv_message.HideSelection = false;
            this.lv_message.Location = new System.Drawing.Point(13, 178);
            this.lv_message.Name = "lv_message";
            this.lv_message.Size = new System.Drawing.Size(775, 260);
            this.lv_message.TabIndex = 4;
            this.lv_message.UseCompatibleStateImageBehavior = false;
            this.lv_message.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "生成时间";
            this.columnHeader1.Width = 130;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "消息类型";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "消息内容";
            this.columnHeader3.Width = 380;
            // 
            // lbl_temperature
            // 
            this.lbl_temperature.AutoSize = true;
            this.lbl_temperature.Location = new System.Drawing.Point(17, 64);
            this.lbl_temperature.Name = "lbl_temperature";
            this.lbl_temperature.Size = new System.Drawing.Size(55, 15);
            this.lbl_temperature.TabIndex = 5;
            this.lbl_temperature.Text = "label1";
            // 
            // lbl_humidity
            // 
            this.lbl_humidity.AutoSize = true;
            this.lbl_humidity.Location = new System.Drawing.Point(17, 103);
            this.lbl_humidity.Name = "lbl_humidity";
            this.lbl_humidity.Size = new System.Drawing.Size(55, 15);
            this.lbl_humidity.TabIndex = 6;
            this.lbl_humidity.Text = "label2";
            // 
            // btn_temperature_update
            // 
            this.btn_temperature_update.Location = new System.Drawing.Point(213, 60);
            this.btn_temperature_update.Name = "btn_temperature_update";
            this.btn_temperature_update.Size = new System.Drawing.Size(100, 25);
            this.btn_temperature_update.TabIndex = 7;
            this.btn_temperature_update.Text = "更新温度";
            this.btn_temperature_update.UseVisualStyleBackColor = true;
            this.btn_temperature_update.Click += new System.EventHandler(this.btn_temperature_update_Click);
            // 
            // tb_temperature
            // 
            this.tb_temperature.Location = new System.Drawing.Point(98, 58);
            this.tb_temperature.Name = "tb_temperature";
            this.tb_temperature.Size = new System.Drawing.Size(100, 25);
            this.tb_temperature.TabIndex = 8;
            // 
            // tb_humidity
            // 
            this.tb_humidity.Location = new System.Drawing.Point(98, 100);
            this.tb_humidity.Name = "tb_humidity";
            this.tb_humidity.Size = new System.Drawing.Size(100, 25);
            this.tb_humidity.TabIndex = 9;
            // 
            // btn_humidity_update
            // 
            this.btn_humidity_update.Location = new System.Drawing.Point(213, 101);
            this.btn_humidity_update.Name = "btn_humidity_update";
            this.btn_humidity_update.Size = new System.Drawing.Size(100, 25);
            this.btn_humidity_update.TabIndex = 10;
            this.btn_humidity_update.Text = "更新湿度";
            this.btn_humidity_update.UseVisualStyleBackColor = true;
            this.btn_humidity_update.Click += new System.EventHandler(this.btn_humidity_update_Click);
            // 
            // btn_whether_to_collect
            // 
            this.btn_whether_to_collect.Location = new System.Drawing.Point(126, 149);
            this.btn_whether_to_collect.Name = "btn_whether_to_collect";
            this.btn_whether_to_collect.Size = new System.Drawing.Size(100, 25);
            this.btn_whether_to_collect.TabIndex = 11;
            this.btn_whether_to_collect.Text = "开启采集";
            this.btn_whether_to_collect.UseVisualStyleBackColor = true;
            this.btn_whether_to_collect.Click += new System.EventHandler(this.btn_whether_to_collect_Click);
            // 
            // chart1
            // 
            chartArea2.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea2);
            legend2.Name = "Legend1";
            this.chart1.Legends.Add(legend2);
            this.chart1.Location = new System.Drawing.Point(13, 444);
            this.chart1.Name = "chart1";
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.Name = "Series1";
            this.chart1.Series.Add(series2);
            this.chart1.Size = new System.Drawing.Size(776, 260);
            this.chart1.TabIndex = 12;
            this.chart1.Text = "chart1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(295, 154);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(97, 15);
            this.label1.TabIndex = 13;
            this.label1.Text = "入库采集比例";
            // 
            // lbl_collected_count
            // 
            this.lbl_collected_count.AutoSize = true;
            this.lbl_collected_count.Location = new System.Drawing.Point(486, 154);
            this.lbl_collected_count.Name = "lbl_collected_count";
            this.lbl_collected_count.Size = new System.Drawing.Size(15, 15);
            this.lbl_collected_count.TabIndex = 14;
            this.lbl_collected_count.Text = "0";
            this.lbl_collected_count.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(465, 154);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(15, 15);
            this.label2.TabIndex = 15;
            this.label2.Text = "/";
            // 
            // lbl_inserted_count
            // 
            this.lbl_inserted_count.AutoSize = true;
            this.lbl_inserted_count.Location = new System.Drawing.Point(444, 154);
            this.lbl_inserted_count.Name = "lbl_inserted_count";
            this.lbl_inserted_count.Size = new System.Drawing.Size(15, 15);
            this.lbl_inserted_count.TabIndex = 16;
            this.lbl_inserted_count.Text = "0";
            this.lbl_inserted_count.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 716);
            this.Controls.Add(this.lbl_inserted_count);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lbl_collected_count);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.btn_whether_to_collect);
            this.Controls.Add(this.btn_humidity_update);
            this.Controls.Add(this.tb_humidity);
            this.Controls.Add(this.tb_temperature);
            this.Controls.Add(this.btn_temperature_update);
            this.Controls.Add(this.lbl_humidity);
            this.Controls.Add(this.lbl_temperature);
            this.Controls.Add(this.lv_message);
            this.Controls.Add(this.btn_send);
            this.Controls.Add(this.btn_connect);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_connect;
        private System.Windows.Forms.Button btn_send;
        private System.Windows.Forms.ListView lv_message;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.Label lbl_temperature;
        private System.Windows.Forms.Label lbl_humidity;
        private System.Windows.Forms.Button btn_temperature_update;
        private System.Windows.Forms.TextBox tb_temperature;
        private System.Windows.Forms.TextBox tb_humidity;
        private System.Windows.Forms.Button btn_humidity_update;
        private System.Windows.Forms.Button btn_whether_to_collect;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lbl_collected_count;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lbl_inserted_count;
    }
}

