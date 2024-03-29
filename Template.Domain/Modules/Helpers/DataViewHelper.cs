﻿using System.Collections.ObjectModel;
using System.Data;

namespace Template.Domain.Modules.Helpers
{
    public static class DataViewHelper
    {
        /// <summary>
        /// EntityコレクションをPivotTableに変換
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TValueType"></typeparam>
        /// <param name="rowName"></param>
        /// <param name="entitiesToLookup"></param>
        /// <param name="getColumn"></param>
        /// <param name="getValue"></param>
        /// <returns></returns>
        public static DataView CreatePivotTable<TEntity, TValueType>(
            string rowName,
            ILookup<string, TEntity> entitiesToLookup,
            Func<TEntity, string> getColumn,
            Func<TEntity, TValueType> getValue)
        {
            //// <利用例>
            ////DataViewItemSource = DataViewHelper.CreatePivotTable<SampleItemTimeEntity, float>(
            ////    "Name",
            ////    sampleItemTimeEntities.ToLookup(o => o.SampleName.Value),
            ////    (i) =>
            ////    {
            ////        return i.SampleItem.Value;
            ////    },
            ////    (i) =>
            ////    {
            ////        return i.SampleTime.Value;
            ////    });

            //// ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ----
            //// 以下をプロパティとして保持する匿名クラスに変換
            //// ID:マトリックス表の行となるプロパティ
            //// Values: Keyにマトリックス表の列となるプロパティ,Valueに値となるプロパティを格納したDictionay
            //// ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ----
            var newVarsFromVars = entitiesToLookup.Select(v => new
            {
                ID = v.Key,
                Values = v.ToDictionary(i => getColumn(i), i => getValue(i))
            });

            //// DataGridのカラムに設定するカラム名を列挙
            var colums = newVarsFromVars.SelectMany(cd => cd.Values).Select(ver => ver.Key).Distinct();

            //// DataGridのItemsSourceとなるDataTableの準備(まずはカラム名をセット)
            var table = new DataTable();
            table.Columns.Add(rowName);
            foreach (var c in colums)
            {
                table.Columns.Add(c, typeof(TValueType));
            }

            //// 実際のデータをセット
            foreach (var newVar in newVarsFromVars)
            {
                int idx = 0;
                var row = table.NewRow();

                //// IDカラムにIDを設定
                row[idx++] = newVar.ID;

                //// カラム名に対応する値が存在する場合はセット
                foreach (var columnName in table.Columns.Cast<DataColumn>().Skip(1).Select(c => c.ColumnName))
                {
                    if (typeof(TValueType) == typeof(string))
                    {
                        //// 文字列の場合
                        row[idx++] = newVar.Values.ContainsKey(columnName) ? newVar.Values[columnName].ToString() : null;
                    }
                    else
                    {
                        //// 数値の場合
                        row[idx++] = newVar.Values.ContainsKey(columnName) ? newVar.Values[columnName].ToString() : DBNull.Value;
                    }
                }

                //// 行データを追加
                table.Rows.Add(row);
            }

            return table.DefaultView;
        }

        /// <summary>
        /// DataViewをEntityに変換
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TValueType"></typeparam>
        /// <param name="dataView"></param>
        /// <param name="rowName"></param>
        /// <param name="createEntity"></param>
        /// <returns></returns>
        public static ObservableCollection<TEntity> ToEntities<TEntity, TValueType>(
            this DataView dataView,
            string rowName,
            Func<string, KeyValuePair<string, string>, TEntity> createEntity)
        {
            //// <利用例>
            ////var entities = DataViewHelper.ToEntities<SampleItemTimeEntity, float>(
            ////    _sampleItemTimeRecords,
            ////    "Name",
            ////    (id, dictionary) =>
            ////    {
            ////        return new SampleItemTimeEntity(id, dictionary.Key, Convert.ToSingle(dictionary.Value));
            ////    });

            var entities = new ObservableCollection<TEntity>();

            DataTable dataTable = dataView.ToTable();

            for (int rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
            {
                //// ID列のデータ
                string idValue = dataTable.Rows[rowIndex][rowName].ToString();

                var itemValues = new Dictionary<string, string>();

                for (int columnIndex = 0; columnIndex < dataTable.Columns.Count; columnIndex++)
                {
                    //// カラム名
                    string colunIndexName = dataTable.Columns[columnIndex].ColumnName;

                    //// 行に対してユニークなIDを示すカラム名は、値を格納せずにループを飛ばす
                    if (colunIndexName == rowName)
                    {
                        continue;
                    }

                    if (dataTable.Rows[rowIndex].IsNull(columnIndex))
                    {
                        itemValues.Add(colunIndexName, null);
                    }
                    else
                    {
                        itemValues.Add(colunIndexName, dataTable.Rows[rowIndex][columnIndex].ToString());
                    }
                }

                foreach (var itemValue in itemValues)
                {
                    if (itemValue.Value == null)
                    {
                        continue;
                    }
                    //entities.Add(new SampleItemTimeEntity(idValue, itemValue.Key, Convert.ToSingle(itemValue.Value)));
                    entities.Add(createEntity(idValue, itemValue));
                }
            }

            return entities;
        }
    }
}
