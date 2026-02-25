input "macros.mp"

beginfig(1);

draw LineSegmentNormalStyle(u*(0,0), u*(0.104173215643618,0.305485117126982));
draw LineSegmentNormalStyle(u*(0.104173215643618,0.305485117126982), u*(0.454767839496069,1.33359430114273));
draw LineSegmentNormalStyle(u*(0,0), u*(0.454767839496069,0));
draw LineSegmentNormalStyle(u*(0.454767839496069,0), u*(1,0));
draw LineSegmentNormalStyle(u*(0.454767839496069,1.33359430114273), u*(0.856785334844951,0.350291628275037));
draw LineSegmentNormalStyle(u*(0.856785334844951,0.350291628275037), u*(1,0));
draw LineSegmentNormalStyle(u*(0.454767839496069,1.33359430114273), u*(0.454767839496069,0));
draw LineSegmentNormalStyle(u*(0,0), u*(0.856785334844951,0.350291628275037));
draw LineSegmentNormalStyle(u*(0.104173215643618,0.305485117126982), u*(1,0));
draw CircleAuxiliaryStyle(u*(0.454767839496069,0.759761724371538), u*0.573832576771193);
draw CircleTheoremStyle(u*(0.477383919748035,0.379880862185769), u*0.380553487096054);
draw PointMarkNormalStyle(u*(0.454767839496069,1.33359430114273));
draw PointMarkNormalStyle(u*(0,0));
draw PointMarkNormalStyle(u*(1,0));
draw PointMarkTheoremStyle(u*(0.454767839496069,0));
draw PointMarkTheoremStyle(u*(0.856785334844951,0.350291628275037));
draw PointMarkTheoremStyle(u*(0.104173215643618,0.305485117126982));
draw PointMarkTheoremStyle(u*(0.454767839496069,0.759761724371538));
draw LabelPoint("A", u*(0.454767839496069,1.33359430114273));
draw LabelPoint("B", u*(0,0));
draw LabelPoint("C", u*(1,0));
draw LabelPoint("D", u*(0.454767839496069,0));
draw LabelPoint("E", u*(0.856785334844951,0.350291628275037));
draw LabelPoint("F", u*(0.104173215643618,0.305485117126982));
draw LabelPoint("G", u*(0.454767839496069,0.759761724371538));
draw TexTextOnTheRight(Triangle_Text("A", "B", "C")
& PerpendicularProjectionOnLineFromPoints_Text("D", "A", "B", "C")
& PerpendicularProjectionOnLineFromPoints_Text("E", "B", "A", "C")
& PerpendicularProjectionOnLineFromPoints_Text("F", "C", "A", "B")
& Circumcenter_Text("G", "A", "E", "F")
& "\par "
& ConcyclicPoints_Text("D", "E", "F", "G")
& "\par\noindent\medskip "
& RankingTable("Symmetry","$0.33$","$10000$","$3333.33$","Level","$0.88$","$1000$","$884.62$","Number of theorems","$28$","$-1$","$-28$","Cyclic quadrilaterals","$3$","$-10$","$-30$")
& "\par\medskip "
& "Total ranking: $4159.95$");

endfig;

beginfig(2);

draw LineSegmentNormalStyle(u*(0,0), u*(0.138004429621675,0.344904634683953));
draw LineSegmentNormalStyle(u*(0.138004429621675,0.344904634683953), u*(0.485367302940817,1.21304390567218));
draw LineSegmentNormalStyle(u*(0,0), u*(0.485367302940817,0));
draw LineSegmentNormalStyle(u*(0.485367302940817,0), u*(1,0));
draw LineSegmentNormalStyle(u*(0.485367302940817,1.21304390567218), u*(0.847466793284368,0.359536962723833));
draw LineSegmentNormalStyle(u*(0.847466793284368,0.359536962723833), u*(1,0));
draw LineSegmentNormalStyle(u*(0,0), u*(0.847466793284368,0.359536962723833));
draw LineSegmentNormalStyle(u*(0.138004429621675,0.344904634683953), u*(1,0));
draw LineSegmentNormalStyle(u*(0.485367302940817,1.21304390567218), u*(0.485367302940817,0));
draw LineSegmentAuxiliaryStyle(u*(0.138004429621675,0.344904634683953), u*(0.485367302940817,0));
draw LineSegmentAuxiliaryStyle(u*(0.138004429621675,0.344904634683953), u*(0.847466793284368,0.359536962723833));
draw LineSegmentAuxiliaryStyle(u*(0.485367302940817,0), u*(0.847466793284368,0.359536962723833));
draw CircleAuxiliaryStyle(u*(0.485367302940817,0.205916606158094), u*0.146121150079349);
draw CircleTheoremStyle(u*(0.485367302940817,0.709480255915137), u*0.503563649757043);
draw PointMarkTheoremStyle(u*(0.485367302940817,1.21304390567218));
draw PointMarkNormalStyle(u*(0,0));
draw PointMarkNormalStyle(u*(1,0));
draw PointMarkTheoremStyle(u*(0.847466793284368,0.359536962723833));
draw PointMarkTheoremStyle(u*(0.138004429621675,0.344904634683953));
draw PointMarkNormalStyle(u*(0.485367302940817,0));
draw PointMarkTheoremStyle(u*(0.485367302940817,0.205916606158094));
draw PointMarkAuxiliaryStyle(u*(0.482354263610654,0.352006688280415));
draw PointMarkAuxiliaryStyle(u*(0.588323009533303,0.102227103408039));
draw PointMarkAuxiliaryStyle(u*(0.382411596348332,0.102227103408039));
draw LabelPoint("A", u*(0.485367302940817,1.21304390567218));
draw LabelPoint("B", u*(0,0));
draw LabelPoint("C", u*(1,0));
draw LabelPoint("D", u*(0.847466793284368,0.359536962723833));
draw LabelPoint("E", u*(0.138004429621675,0.344904634683953));
draw LabelPoint("F", u*(0.485367302940817,0));
draw LabelPoint("G", u*(0.485367302940817,0.205916606158094));
draw TexTextOnTheRight(Triangle_Text("A", "B", "C")
& PerpendicularProjectionOnLineFromPoints_Text("D", "B", "A", "C")
& PerpendicularProjectionOnLineFromPoints_Text("E", "C", "A", "B")
& PerpendicularProjectionOnLineFromPoints_Text("F", "A", "B", "C")
& Incenter_Text("G", "D", "E", "F")
& "\par "
& ConcyclicPoints_Text("A", "D", "E", "G")
& "\par\noindent\medskip "
& RankingTable("Symmetry","$0.33$","$10000$","$3333.33$","Level","$0.88$","$1000$","$884.62$","Number of theorems","$39$","$-1$","$-39$","Cyclic quadrilaterals","$5$","$-10$","$-50$")
& "\par\medskip "
& "Total ranking: $4128.95$");

endfig;

beginfig(3);

draw LineSegmentNormalStyle(u*(0,0), u*(0.0582540083538495,0.234223139045994));
draw LineSegmentNormalStyle(u*(0.0582540083538495,0.234223139045994), u*(0.325870647590364,1.31023509211486));
draw LineSegmentNormalStyle(u*(0,0), u*(0.325870647590364,0));
draw LineSegmentNormalStyle(u*(0.325870647590364,0), u*(1,0));
draw LineSegmentNormalStyle(u*(0.325870647590364,1.31023509211486), u*(0.790688365532488,0.406817249073894));
draw LineSegmentNormalStyle(u*(0.790688365532488,0.406817249073894), u*(1,0));
draw LineSegmentNormalStyle(u*(0.325870647590364,1.31023509211486), u*(0.325870647590364,0));
draw LineSegmentNormalStyle(u*(0,0), u*(0.790688365532488,0.406817249073894));
draw LineSegmentNormalStyle(u*(0.0582540083538495,0.234223139045994), u*(1,0));
draw CircleAuxiliaryStyle(u*(0.162935323795182,0.655117546057432), u*0.675075491254535);
draw CircleTheoremStyle(u*(0.412935323795182,0.369474718104423), u*0.379594290211263);
draw PointMarkNormalStyle(u*(0.325870647590364,1.31023509211486));
draw PointMarkNormalStyle(u*(0,0));
draw PointMarkNormalStyle(u*(1,0));
draw PointMarkTheoremStyle(u*(0.325870647590364,0));
draw PointMarkTheoremStyle(u*(0.790688365532488,0.406817249073894));
draw PointMarkTheoremStyle(u*(0.0582540083538495,0.234223139045994));
draw PointMarkTheoremStyle(u*(0.162935323795182,0.655117546057432));
draw LabelPoint("A", u*(0.325870647590364,1.31023509211486));
draw LabelPoint("B", u*(0,0));
draw LabelPoint("C", u*(1,0));
draw LabelPoint("D", u*(0.325870647590364,0));
draw LabelPoint("E", u*(0.790688365532488,0.406817249073894));
draw LabelPoint("F", u*(0.0582540083538495,0.234223139045994));
draw LabelPoint("G", u*(0.162935323795182,0.655117546057432));
draw TexTextOnTheRight(Triangle_Text("A", "B", "C")
& PerpendicularProjectionOnLineFromPoints_Text("D", "A", "B", "C")
& PerpendicularProjectionOnLineFromPoints_Text("E", "B", "A", "C")
& PerpendicularProjectionOnLineFromPoints_Text("F", "C", "A", "B")
& Circumcenter_Text("G", "A", "B", "D")
& "\par "
& ConcyclicPoints_Text("D", "E", "F", "G")
& "\par\noindent\medskip "
& RankingTable("Level","$0.88$","$1000$","$884.62$","Symmetry","$0$","$10000$","$0$","Number of theorems","$28$","$-1$","$-28$","Cyclic quadrilaterals","$3$","$-10$","$-30$")
& "\par\medskip "
& "Total ranking: $826.62$");

endfig;

end