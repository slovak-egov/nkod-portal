type Props = {
    href: string;
};

export default function ExternalLink(props: Props) {
    const { href } = props;
    let title = href;
    if (href) {
        const url = new URL(href);
        if (url.hostname) {
            title = url.hostname;
        }
    }
    return (
        <a href={href} target="_blank" className="idsk-card-title govuk-link" rel="noopener noreferrer">
            {title}
        </a>
    );
}
