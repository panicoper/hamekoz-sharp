﻿//
//  Search.cs
//
//  Author:
//       Claudio Rodrigo Pereyra Diaz <claudiorodrigo@pereyradiaz.com.ar>
//
//  Copyright (c) 2015 Hamekoz
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System.Collections.Generic;
using System.Linq;
using Hamekoz.Core;
using Xwt;

namespace Hamekoz.UI
{
	public class Search<T>: VBox where T : ISearchable
	{
		public IList<T> List {
			get;
			set;
		}

		SearchTextEntry text = new SearchTextEntry ();
		readonly ListViewDinamic<T> filterList = new ListViewDinamic<T> {
			MinWidth = 600,
			MinHeight = 300,
		};

		public Search ()
		{
			text.Activated += delegate {
				filterList.List = List
					.Where (r => r.ToSearchString ().ToUpper ().Contains (text.Text.ToUpper ()))
					.ToList ();
			};
			text.SetCompletions (typeof(T).GetProperties ().Select (p => p.Name).ToArray<string> ());

			PackStart (text);
			PackStart (filterList, true);
		}

		public T Selected {
			get {
				return filterList.Current;
			}
		}
	}
}

