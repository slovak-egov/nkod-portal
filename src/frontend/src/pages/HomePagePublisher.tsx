import { Link } from "react-router-dom";
import { Publisher } from "../client";

type Props = {
    publisher: Publisher;
}

export default function HomePagePublisher(props: Props) {
    const themes = props.publisher.themes ? Object.entries(props.publisher.themes).filter((_, c) => c > 0).sort((a, b) => b[1] - a[1]).slice(0, 3).map(v => ({name: v[0], count: v[1]})) : [];

    return <div className="idsk-crossroad__item ">
        <Link to={'/datasety?publisher=' + encodeURIComponent(props.publisher.key)} className="govuk-link idsk-crossroad-title" title={props.publisher.name} aria-hidden="false">{props.publisher.name}
        </Link>
        <p className="idsk-crossroad-subtitle" aria-hidden="false">Poskytovateľ zvrejňuje {props.publisher.datasetCount} datasetov{themes.length > 0 ? <>, najviac z oblastí {
            themes.map((t, i) => <span key={i}>{t.name} ({t.count}){i < themes.length - 1 ? ', ' : null}</span>)
        }.</> : null}
        </p>
        <hr className="idsk-crossroad-line" aria-hidden="true" />
    </div>
}