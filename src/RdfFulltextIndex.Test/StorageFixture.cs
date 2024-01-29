using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.RdfFulltextIndex.Test
{
    public class StorageFixture 
    {
        public StorageFixture() 
        {
            Index = new FulltextIndex(new DefaultLanguagesSource());
            Index.Index(new[] { new FileState(
                new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, "Pub1", true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
                @"@prefix dcat: <http://www.w3.org/ns/dcat#> .
@prefix dct: <http://purl.org/dc/terms/> .
@prefix foaf: <http://xmlns.com/foaf/0.1/> .
@prefix vcard2006: <http://www.w3.org/2006/vcard/ns#> .
@prefix xsd: <http://www.w3.org/2001/XMLSchema#> .
@prefix filetype: <http://publications.europa.eu/resource/authority/file-type/> .
@prefix application: <http://www.iana.org/assignments/media-types/application/> .
@prefix freq: <http://publications.europa.eu/resource/authority/frequency/> .
@prefix theme: <http://publications.europa.eu/resource/authority/data-theme/> .
@prefix continent: <http://publications.europa.eu/resource/authority/continent/> .
@prefix euroVoc: <http://eurovoc.europa.eu/> .
@prefix leg: <https://data.gov.sk/def/ontology/legislation/> .

<https://data.gov.sk/set/vld> a dcat:Dataset ;
    dct:title ""Cestovné poriadky""@sk, ""Public transport timetables""@en;
    dct:description ""Obsahom datasetu sú schválené a aktuálne platné cestovné poriadky verejné linkové dopravy poskytnuté do Centrálneho informačního systému Cestovné poriadky ve strojovo spracovateľnom formáte.""@sk ;
    dct:description ""This dataset contains approved timetables and timetables in effect for public transport entered into the state-wide timetable information system.""@en ;
    dct:publisher <https://data.gov.sk/legal-subject/30416094> ;
    dct:type <https://data.gov.sk/def/dataset-type/1> ;
    dcat:theme theme:TRAN ;
    dct:accrualPeriodicity freq:WEEKLY_3 ;
    dcat:keyword ""autobusy""@sk, ""cestovné poriadky""@sk, ""verejná linková doprava""@sk, ""timetable""@en, ""bus""@en, ""public transport""@en ;
    dct:spatial continent:EUROPE ;
    dct:temporal [
        a dct:PeriodOfTime ;
        dcat:startDate ""2009-01-01""^^xsd:date ;
        dcat:endDate ""2017-12-31""^^xsd:date
    ] ;
    dcat:contactPoint [
        a vcard2006:Organization ;
        vcard2006:fn ""Ministerstvo dopravy, Odbor verejnej dopravy""@sk, ""Ministry of Transport""@en ;
        vcard2006:hasEmail ""mailto:info@mindop.sk""
    ] ;
    foaf:page <https://www.mindop.sk/cestovne-poriadky-info> ;
    dct:conformsTo <https://data.gov.sk/def/ontology/transport/> ;
    dcat:theme euroVoc:4512 ;
    dcat:spatialResolutionInMeters 12.0 ;
    dcat:temporalResolution ""P1D""^^xsd:duration ;
    dcat:distribution <https://data.gov.sk/set/vld/resource/csv> ;
    dcat:distribution <https://data.gov.sk/set/vld/resource/sparql> .
") });
        }

        public FulltextIndex Index { get; set; }
    }
}
