<?xml version="1.0"?>
<!--
	stylesheet to convert a FHIR resource into a HTML rendering for display
	(based on the IE default stylesheet)
	Author:  Brian Postlethwaite (brian_pos@hotmail.com)
-->

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:fhir="http://hl7.org/fhir" >
	<xsl:output omit-xml-declaration="yes" method="html" />
	<xsl:template match="/">
		<!--<STYLE>
		/*-->
		<!-- container for expanding/collapsing content -->
		<!--*/
		.c {
		}
		/*-->
		<!-- button - contains +/-/nbsp -->
		<!--*/
		.b {
		color: red;
		font-weight: bold;
		text-decoration: none;
		}
		/*-->
		<!-- element container -->
		<!--*/
		.e {
		margin-left: 1em;
		text-indent: -1em;
		margin-right: 1em;
		}
		/*-->
		<!-- comment or cdata -->
		<!--*/
		.k {
		margin-left: 1em;
		text-indent: -1em;
		margin-right: 1em;
		}
		/*-->
		<!-- tag -->
		<!--*/
		.t {
		color: #990000;
		}
		/*-->
		<!-- tag in xsl namespace -->
		<!--*/
		.xt {
		color: #990099;
		}
		/*-->
		<!-- attribute in xml or xmlns namespace -->
		<!--*/
		.ns {
		color: red;
		}
		/*-->
		<!-- markup characters -->
		<!--*/
		.m {
		color: blue;
		}
		/*-->
		<!-- text node -->
		<!--*/
		.tx {
		font-weight: bold;
		}
		/*-->
		<!-- multi-line (block) cdata -->
		<!--*/
		.db {
		text-indent: 0;
		margin-left: 1em;
		margin-top: 0;
		margin-bottom: 0;
		padding-left: .3em;
		border-left: 1px solid #CCCCCC;
		font: small Courier;
		}
		/*-->
		<!-- single-line (inline) cdata -->
		<!--*/
		.di {
		font: small Courier;
		}
		/*-->
		<!-- DOCTYPE declaration -->
		<!--*/
		.d {
		color: blue;
		}
		/*-->
		<!-- pi -->
		<!--*/
		.pi {
		color: blue;
		}
		/*-->
		<!-- multi-line (block) comment -->
		<!--*/
		.cb {
		text-indent: 0;
		margin-left: 1em;
		margin-top: 0;
		margin-bottom: 0;
		padding-left: .3em;
		font: small Courier;
		color: #888888;
		}
		/*-->
		<!-- single-line (inline) comment -->
		<!--*/
		.ci {
		font: small Courier;
		color: #888888;
		}

		PRE {
		margin: 0;
		display: inline;
		}
	</STYLE>-->
		<xsl:apply-templates>
			<xsl:with-param name="margin" select="number(0)" />
		</xsl:apply-templates>
	</xsl:template>

	<xsl:template match="fhir:photo/fhir:data/@value">
		<SPAN class="t"> value</SPAN>
		<SPAN class="m">="</SPAN>
		<img alt="Photo Attachment" width="200">
			<xsl:attribute name="src">
				<xsl:text>data:image/jpeg;base64,</xsl:text>
				<xsl:value-of select="."/>
			</xsl:attribute>
		</img>
		<SPAN class="m">"</SPAN>
	</xsl:template>


	<!-- Templates for each node type follows.  The output of
each template has a similar structure to enable script to
walk the result tree easily for handling user
interaction. -->

	<!-- Template for pis not handled elsewhere -->
	<xsl:template match="processing-instruction()">
		<DIV class="e">
			<SPAN class="b">&#160;</SPAN>
			<SPAN class="m">&lt;?</SPAN>
			<SPAN class="pi">
				<xsl:value-of
select="name()"/>&#160;<xsl:value-of select="."/>
			</SPAN>
			<SPAN
class="m">?&gt;</SPAN>
		</DIV>
	</xsl:template>


	<!-- Template for attributes not handled elsewhere -->
	<xsl:template match="@*" xml:space="preserve"><SPAN><xsl:attribute
name="class"><xsl:if
   test="starts-with(name(),'xsl:')">x</xsl:if>t</xsl:attribute><xsl:text> </xsl:text><xsl:value-of select="name()" /></SPAN><SPAN class="m">="</SPAN><B><xsl:value-of select="."/></B><SPAN class="m">"</SPAN></xsl:template>

	<!-- Template for text nodes -->
	<xsl:template match="text()"></xsl:template>


	<!-- Note that in the following templates for comments
and cdata, by default we apply a style appropriate for
single line content (e.g. non-expandable, single line
display).  But we also inject the attribute 'id="clean"' and
a script call 'f(clean)'.  As the output is read by the
browser, it executes the function immediately.  The function
checks to see if the comment or cdata has multi-line data,
in which case it changes the style to a expandable,
multi-line display.  Performing this switch in the DHTML
instead of from script in the XSL increases the performance
of the style sheet, especially in the browser's asynchronous
case -->

	<!-- Template for comment nodes -->
	<xsl:template match="comment()">
		<DIV class="k">
			<SPAN>
				<SPAN class="m">&lt;!--</SPAN>
			</SPAN>
			<SPAN id="clean" class="ci">
				<PRE>
					<xsl:value-of select="."/>
				</PRE>
			</SPAN>
			<SPAN class="b">&#160;</SPAN>
			<SPAN class="m">--&gt;</SPAN>
		</DIV>
	</xsl:template>


	<!-- Note the following templates for elements may
examine children.  This harms to some extent the ability to
process a document asynchronously - we can't process an
element until we have read and examined at least some of its
children.  Specifically, the first element child must be
read before any template can be chosen.  And any element
that does not have element children must be read completely
before the correct template can be chosen. This seems an
acceptable performance loss in the light of the formatting
possibilities available when examining children. -->

	<!-- Template for elements not handled elsewhere (leaf nodes) -->
	<xsl:template match="*">
		<xsl:param name="margin" />
		<DIV class="e">
			<xsl:attribute name="style">
				padding-left: <xsl:value-of select="$margin"/>px
			</xsl:attribute>
			<SPAN class="m">&lt;</SPAN>
			<SPAN class="t">
				<xsl:value-of select="name()"/>
			</SPAN>
			<xsl:apply-templates select="@*"/>
			<SPAN class="m">/&gt;</SPAN>
		</DIV>
	</xsl:template>

	<!-- Template for elements with comment, pi and/or cdata children -->
	<xsl:template match="*[comment() | processing-instruction()]"  xml:space="preserve">
  <DIV class="e">
  <DIV class="c"><!--<A class="b"></A>--> <SPAN
	  class="m">&lt;</SPAN><SPAN><xsl:attribute
name="class">t</xsl:attribute>
	  <xsl:value-of
			  select="name()"/></SPAN><xsl:apply-templates select="@*"/> 
	  <SPAN
		  class="m">&gt;</SPAN></DIV>
  <DIV><xsl:apply-templates/>
  <DIV><SPAN class="b">&#160;</SPAN> <SPAN
class="m">&lt;/</SPAN><SPAN><xsl:attribute name="class">t</xsl:attribute>
	  <xsl:value-of
				  select="name()"/></SPAN><SPAN class="m">&gt;</SPAN></DIV>
  </DIV></DIV>
</xsl:template>

	<!-- Template for elements with only text children -->
	<xsl:template match="*[text() and not(comment() |
processing-instruction())]" xml:space="preserve">
  <DIV class="e"><DIV STYLE="margin-left:1em;text-indent:-2em">
  <SPAN class="b">&#160;</SPAN> <SPAN
class="m">&lt;</SPAN><SPAN><xsl:attribute
	  name="class">t</xsl:attribute>
	  <xsl:value-of
			  select="name()"/></SPAN><xsl:apply-templates select="@*"/>
  <SPAN class="m">&gt;</SPAN><SPAN class="tx">
	  <xsl:value-of
			  select="."/></SPAN><SPAN class="m">&lt;/</SPAN>
	  <SPAN><xsl:attribute
		name="class">t</xsl:attribute>
	  <xsl:value-of
			  select="name()"/></SPAN><SPAN class="m">&gt;</SPAN>
  </DIV></DIV>
</xsl:template>

	<!-- Template for elements with element children -->
	<xsl:template match="*[*]" xml:space="preserve"><xsl:param name="margin" />
	<xsl:variable name="newmargin"><xsl:choose><xsl:when test="string-length($margin)=0 or $margin='NaN' or string(number($margin))='NaN'"><xsl:number value="0"/></xsl:when><xsl:otherwise><xsl:value-of select="$margin"/></xsl:otherwise></xsl:choose></xsl:variable>
	<!--1:[<xsl:value-of select="number(0)"/>] - 2:[<xsl:value-of select="$margin"/>] - 3:[<xsl:value-of select="number($margin)"/>] - 4:[<xsl:value-of select="$newmargin"/>]-->
<DIV class="e"><xsl:attribute name="style">padding-left: <xsl:value-of select="number($newmargin)"/>px</xsl:attribute><SPAN class="m">&lt;</SPAN><SPAN class="t"><xsl:value-of select="name()"/></SPAN><xsl:apply-templates select="@*"/> <SPAN class="m">&gt;</SPAN></DIV>
<xsl:apply-templates>
<xsl:with-param name="margin" select="number($newmargin) + 20" />
</xsl:apply-templates>
<DIV class="e"><xsl:attribute name="style">padding-left: <xsl:value-of select="number($newmargin)"/>px</xsl:attribute><SPAN class="m">&lt;/</SPAN><SPAN class="t"><xsl:value-of select="name()"/></SPAN><SPAN class="m">&gt;</SPAN></DIV>
</xsl:template>

</xsl:stylesheet>
