import IdSkModule from "./IdSkModule"

interface IProps
{
    header: string
    links: ILink[]
}

interface ILink
{
    title: string
    url: string
}

export default function RelatedContent(props: IProps)
{
    return <IdSkModule className="idsk-related-content" moduleType="idsk-related-content">
        <hr className="idsk-related-content__line" aria-hidden="true" />
        <h4 className="idsk-related-content__heading govuk-heading-s">
            {props.header}
        </h4>
        <ul className="idsk-related-content__list govuk-list">
            {props.links.map(l => <li className="idsk-related-content__list-item" key={l.url}>
                <a className="idsk-related-content__link" href={l.url} title={l.title}>{l.title}</a>
            </li>)}
        </ul>
    </IdSkModule>
}